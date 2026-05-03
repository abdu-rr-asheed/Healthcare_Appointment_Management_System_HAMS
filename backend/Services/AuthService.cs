using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _cache;

        // Redis key prefixes and TTL constants
        private const string MfaOtpPrefix      = "mfa:otp:";      // mfa:otp:{userId}      → 6-digit code
        private const string MfaPendingPrefix  = "mfa:pending:";   // mfa:pending:{userId}  → userId (presence = valid session)
        private const string MfaAttemptsPrefix = "mfa:attempts:";  // mfa:attempts:{userId} → failed attempt count
        private static readonly TimeSpan MfaTtl = TimeSpan.FromMinutes(10);
        private const int MfaMaxAttempts = 3;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IAuditService auditService,
            IDistributedCache cache)
        {
            _context = context;
            _configuration = configuration;
            _auditService = auditService;
            _cache = cache;
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
            await _context.SaveChangesAsync(); // persist refresh token added by GenerateTokens
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

            // When MFA is enabled, generate an OTP, store it in Redis, and
            // return a challenge response — NO tokens are issued at this stage.
            if (user.TwoFactorEnabled)
            {
                var userId = user.Id.ToString();
                var otp = GenerateMfaOtp();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = MfaTtl
                };

                // OTP code — what the user must submit
                await _cache.SetStringAsync(MfaOtpPrefix + userId, otp, cacheOptions);

                // Pending-session marker — proves this userId went through password
                // auth legitimately. VerifyMfaAsync rejects requests that lack it.
                await _cache.SetStringAsync(MfaPendingPrefix + userId, userId, cacheOptions);

                // Reset any leftover attempt counter from a prior session
                await _cache.RemoveAsync(MfaAttemptsPrefix + userId);

                // TODO: replace with a real SMS dispatch once NotificationService
                //       exposes a SendMfaSmsAsync method.
                // For now the OTP is written to the structured log so developers
                // can verify the flow without an SMS gateway configured.
                // REMOVE this log line before going to production.
                Console.WriteLine($"[MFA-OTP] userId={userId} code={otp}");

                return new AuthResponse
                {
                    AccessToken = null,
                    RefreshToken = null,
                    User = MapToUserDto(user),
                    RequiresMfa = true,
                    MfaUserId = userId
                };
            }

            var tokens = GenerateTokens(user);
            await _context.SaveChangesAsync(); // persist refresh token added by GenerateTokens
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = false,
                MfaUserId = null
            };
        }

        public async Task<AuthResponse> VerifyMfaAsync(string userId, string code)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new UnauthorizedAccessException("Invalid user identifier");
            }

            var user = await _context.Users.FindAsync(userGuid);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            // ── 1. Check pending-session key ─────────────────────────────────────
            // This key is only present when LoginAsync completed password auth and
            // issued an MFA challenge. Its absence means either the session expired
            // or the caller never went through login at all.
            var pendingKey  = MfaPendingPrefix  + userId;
            var otpKey      = MfaOtpPrefix      + userId;
            var attemptsKey = MfaAttemptsPrefix + userId;

            var pendingSession = await _cache.GetStringAsync(pendingKey);
            if (pendingSession == null)
            {
                await _auditService.LogAsync(
                    userId, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
                    "MfaVerify", "User", userGuid, "", "", "Failure-NoSession");
                throw new UnauthorizedAccessException("MFA session not found or has expired. Please log in again.");
            }

            // ── 2. Retrieve stored OTP ───────────────────────────────────────────
            var storedOtp = await _cache.GetStringAsync(otpKey);
            if (storedOtp == null)
            {
                // OTP key expired (shouldn't happen if TTLs are identical, but be safe)
                await _cache.RemoveAsync(pendingKey);
                await _auditService.LogAsync(
                    userId, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
                    "MfaVerify", "User", userGuid, "", "", "Failure-OtpExpired");
                throw new UnauthorizedAccessException("MFA code has expired. Please log in again.");
            }

            // ── 3. Constant-time comparison ──────────────────────────────────────
            // PadRight(6) ensures both byte arrays are the same length regardless
            // of what the caller supplied, keeping comparison time constant.
            var storedBytes   = Encoding.UTF8.GetBytes(storedOtp.PadRight(6));
            var suppliedBytes = Encoding.UTF8.GetBytes((code ?? "").PadRight(6));
            var codesMatch    = CryptographicOperations.FixedTimeEquals(storedBytes, suppliedBytes);

            if (!codesMatch)
            {
                // ── 3a. Increment attempt counter ────────────────────────────────
                var attemptsRaw = await _cache.GetStringAsync(attemptsKey);
                var attempts = int.TryParse(attemptsRaw, out var parsed) ? parsed + 1 : 1;

                if (attempts >= MfaMaxAttempts)
                {
                    // Too many failures — wipe the entire MFA session.
                    // The user must start over with a fresh login.
                    await _cache.RemoveAsync(otpKey);
                    await _cache.RemoveAsync(pendingKey);
                    await _cache.RemoveAsync(attemptsKey);

                    await _auditService.LogAsync(
                        userId, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
                        "MfaVerify", "User", userGuid, "", "", "Failure-LockedOut");
                    throw new UnauthorizedAccessException(
                        "Too many incorrect attempts. Your MFA session has been invalidated. Please log in again.");
                }

                // Persist the updated counter with the same TTL
                await _cache.SetStringAsync(attemptsKey, attempts.ToString(),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = MfaTtl });

                await _auditService.LogAsync(
                    userId, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
                    "MfaVerify", "User", userGuid, "", "", $"Failure-InvalidCode-Attempt{attempts}");
                throw new UnauthorizedAccessException(
                    $"Invalid MFA code. {MfaMaxAttempts - attempts} attempt(s) remaining.");
            }

            // ── 4. Success — invalidate all three MFA keys ───────────────────────
            await _cache.RemoveAsync(otpKey);
            await _cache.RemoveAsync(pendingKey);
            await _cache.RemoveAsync(attemptsKey);

            await _auditService.LogAsync(
                userId, $"{user.FirstName} {user.LastName}", user.Role.ToString(),
                "MfaVerify", "User", userGuid, "", "", "Success");

            var tokens = GenerateTokens(user);
            await _context.SaveChangesAsync(); // persist refresh token
            return new AuthResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = tokens.ExpiresAt,
                User = MapToUserDto(user),
                RequiresMfa = false,
                MfaUserId = null
            };
        }

        /// <summary>
        /// Generates a cryptographically secure 6-digit numeric OTP.
        /// Uses RandomNumberGenerator to avoid modulo bias: keeps drawing
        /// until the value falls inside the largest multiple-of-1000000 that
        /// fits in a uint, then takes the remainder.
        /// </summary>
        private static string GenerateMfaOtp()
        {
            const uint range = 1_000_000; // 000000–999999
            // Largest multiple of range that fits in a uint, to avoid bias
            const uint limit = uint.MaxValue - (uint.MaxValue % range);
            Span<byte> buf = stackalloc byte[4];
            uint value;
            do
            {
                RandomNumberGenerator.Fill(buf);
                value = BitConverter.ToUInt32(buf);
            } while (value >= limit);

            return (value % range).ToString("D6");
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