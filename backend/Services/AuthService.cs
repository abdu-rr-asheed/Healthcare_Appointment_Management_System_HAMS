using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;

namespace HAMS.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.NhsNumber == request.NhsNumber))
            {
                throw new Exception("NHS number already registered");
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new Exception("Email already registered");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                NhsNumber = request.NhsNumber,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                TwoFactorEnabled = request.MfaEnabled,
                IsActive = true,
                Role = UserRole.Patient,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Address = request.Address,
                City = request.City,
                Postcode = request.Postcode,
                SmsOptIn = request.SmsOptIn,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                CreatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user.Id.ToString(),
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString(),
                "UserLogin",
                "User",
                user.Id,
                "",
                "",
                "Success"
            );

            var tokens = GenerateTokens(user);
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = user.TwoFactorEnabled,
                MfaUserId = user.TwoFactorEnabled ? user.Id.ToString() : null
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NhsNumber == request.NhsNumber);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                user.Id.ToString(),
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString(),
                "UserLogin",
                "User",
                user.Id,
                "",
                "",
                "Success"
            );

            var tokens = GenerateTokens(user);
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = user.TwoFactorEnabled,
                MfaUserId = user.TwoFactorEnabled ? user.Id.ToString() : null
            };
        }

        public async Task<AuthResponse> VerifyMfaAsync(string userId, string code)
        {
            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var tokens = GenerateTokens(user);
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = false
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (storedToken == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            if (!storedToken.IsActive)
            {
                throw new UnauthorizedAccessException("Refresh token is expired or revoked");
            }

            var user = storedToken.User;
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive");
            }

            var newRefreshToken = GenerateRefreshToken(user.Id.ToString());

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken.Token;

            _context.RefreshTokens.Update(storedToken);
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var tokens = GenerateTokens(user);

            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = false
            };
        }

        public async Task LogoutAsync(string userId)
        {
            await _auditService.LogAsync(
                userId,
                "User",
                "",
                "UserLogout",
                "User",
                Guid.Parse(userId),
                "",
                "",
                "Success"
            );
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _context.Users.FindAsync(Guid.Parse(userId));
            return user != null ? MapToUserDto(user) : null;
        }

        public string? GetUserRoleFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                return jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            }
            catch
            {
                return null;
            }
        }

        private (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken(user.Id.ToString());

            _context.RefreshTokens.Add(refreshToken);

            return (accessToken, refreshToken.Token, token.ValidTo);
        }

        private RefreshToken GenerateRefreshToken(string userId)
        {
            var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 7);

            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userId),
                Token = Convert.ToBase64String(randomBytes),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow
            };
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                NhsNumber = user.NhsNumber,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                TwoFactorEnabled = user.TwoFactorEnabled,
                IsActive = user.IsActive
            };
        }
    }
}