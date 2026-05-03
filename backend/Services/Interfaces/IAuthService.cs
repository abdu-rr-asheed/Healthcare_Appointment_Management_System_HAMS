using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> VerifyMfaAsync(string userId, string code);
        Task ResendMfaAsync(string userId);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string userId);
        Task<UserDto?> GetUserByIdAsync(string userId);
        string? GetUserRoleFromToken(string token);
    }
}