namespace HAMS.API.Models.DTOs.Responses
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
        public bool RequiresMfa { get; set; }
        public string? MfaUserId { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string NhsNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        public bool IsActive { get; set; }
    }
}