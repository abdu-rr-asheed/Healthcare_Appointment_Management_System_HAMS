using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Services.Interfaces;
using System.Security.Claims;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _env;

        // Access token lifetime must match JwtSettings:ExpiryMinutes (60 min).
        // Refresh token lifetime must match JwtSettings:RefreshTokenExpiryDays (7 days).
        private static readonly TimeSpan AccessTokenMaxAge  = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan RefreshTokenMaxAge = TimeSpan.FromDays(7);

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger,
            IWebHostEnvironment env)
        {
            _authService = authService;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Writes access_token and refresh_token as HttpOnly cookies and removes
        /// the raw token strings from the response body so they never appear in JS.
        /// </summary>
        private void SetAuthCookies(AuthResponse result)
        {
            var isProduction = !_env.IsDevelopment();

            // Base options shared by both cookies
            var baseOptions = new CookieOptions
            {
                HttpOnly = true,
                // Secure=true forces HTTPS-only; disabled for plain-HTTP local dev.
                Secure   = isProduction,
                // Strict prevents the cookie being sent on cross-site requests in
                // production (same-origin via nginx). Lax is used in development
                // where the Angular dev-server and API are on different ports.
                SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
                Path     = "/"
            };

            Response.Cookies.Append("access_token", result.AccessToken ?? string.Empty,
                new CookieOptions
                {
                    HttpOnly = baseOptions.HttpOnly,
                    Secure   = baseOptions.Secure,
                    SameSite = baseOptions.SameSite,
                    Path     = "/",
                    MaxAge   = AccessTokenMaxAge
                });

            // Scope the refresh-token cookie to the one endpoint that needs it.
            Response.Cookies.Append("refresh_token", result.RefreshToken ?? string.Empty,
                new CookieOptions
                {
                    HttpOnly = baseOptions.HttpOnly,
                    Secure   = baseOptions.Secure,
                    SameSite = baseOptions.SameSite,
                    Path     = "/api/auth/refresh-token",
                    MaxAge   = RefreshTokenMaxAge
                });

            // Strip tokens from the JSON body — they are now in cookies only.
            result.AccessToken  = null;
            result.RefreshToken = null;
        }

        /// <summary>
        /// Expires both auth cookies immediately, forcing the browser to discard them.
        /// </summary>
        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("access_token",  new CookieOptions { Path = "/" });
            Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth/refresh-token" });
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                // Only set cookies when the account does not require an MFA step.
                if (!result.RequiresMfa)
                {
                    SetAuthCookies(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                // MFA-required responses carry no tokens yet; cookies are set after
                // the second factor is verified in the verify-mfa endpoint.
                if (!result.RequiresMfa)
                {
                    SetAuthCookies(result);
                }
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login failed");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during login" });
            }
        }

        [HttpPost("resend-mfa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResendMfa([FromBody] ResendMfaRequest request)
        {
            try
            {
                await _authService.ResendMfaAsync(request.UserId);
                return Ok(new { message = "A new verification code has been sent." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Resend MFA failed — no active session");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resend MFA failed");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred while resending the code" });
            }
        }

        [HttpPost("verify-mfa")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerificationRequest request)
        {
            try
            {
                var result = await _authService.VerifyMfaAsync(request.UserId, request.Code);
                SetAuthCookies(result);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "MFA verification failed");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MFA verification failed");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during MFA verification" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    await _authService.LogoutAsync(userId);
                }
                ClearAuthCookies();
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during logout" });
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // Read the refresh token from the HttpOnly cookie — it is never
                // sent in the request body since client JS cannot access it.
                var refreshToken = Request.Cookies["refresh_token"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new ErrorResponse { Message = "No refresh token present." });
                }

                var result = await _authService.RefreshTokenAsync(refreshToken);
                SetAuthCookies(result);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Token refresh failed");
                ClearAuthCookies(); // expired/revoked — force re-login
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during token refresh" });
            }
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized();
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current user");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }
    }

    public class MfaVerificationRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;
    }

    public class ResendMfaRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
    }

}