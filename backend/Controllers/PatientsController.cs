using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using System.Security.Claims;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(ApplicationDbContext context, ILogger<PatientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(PatientProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentPatient()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));

                if (patient == null)
                {
                    return NotFound(new ErrorResponse { Message = "Patient not found" });
                }

                var result = new PatientProfileDto
                {
                    Id = patient.Id,
                    FirstName = patient.User.FirstName,
                    LastName = patient.User.LastName,
                    Email = patient.User.Email,
                    PhoneNumber = patient.User.PhoneNumber ?? string.Empty,
                    NhsNumber = patient.User.NhsNumber ?? string.Empty,
                    DateOfBirth = patient.User.DateOfBirth,
                    SmsOptIn = patient.SmsOptIn
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get patient profile");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(PatientProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateCurrentPatient([FromBody] UpdatePatientRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));

                if (patient == null)
                {
                    return NotFound(new ErrorResponse { Message = "Patient not found" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    if (!string.IsNullOrEmpty(request.FirstName))
                        user.FirstName = request.FirstName;
                    if (!string.IsNullOrEmpty(request.LastName))
                        user.LastName = request.LastName;
                    if (!string.IsNullOrEmpty(request.Email))
                        user.Email = request.Email;
                    if (!string.IsNullOrEmpty(request.PhoneNumber))
                        user.PhoneNumber = request.PhoneNumber;
                }

                if (request.SmsOptIn.HasValue)
                {
                    patient.SmsOptIn = request.SmsOptIn.Value;
                }

                await _context.SaveChangesAsync();

                var result = new PatientProfileDto
                {
                    Id = patient.Id,
                    FirstName = patient.User.FirstName,
                    LastName = patient.User.LastName,
                    Email = patient.User.Email,
                    PhoneNumber = patient.User.PhoneNumber ?? string.Empty,
                    NhsNumber = patient.User.NhsNumber ?? string.Empty,
                    DateOfBirth = patient.User.DateOfBirth,
                    SmsOptIn = patient.SmsOptIn
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update patient profile");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("me/notifications")]
        [ProducesResponseType(typeof(IEnumerable<PatientNotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPatientNotifications()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == Guid.Parse(userId));

                if (patient == null)
                {
                    return NotFound(new ErrorResponse { Message = "Patient not found" });
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == patient.UserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                var result = notifications.Select(n => new PatientNotificationDto
                {
                    Id = n.Id,
                    Subject = n.Subject,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.Status == NotificationStatus.Delivered || n.Status == NotificationStatus.Sent,
                    CreatedAt = n.CreatedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notifications");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPut("me/notifications/{id}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkNotificationAsRead(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == Guid.Parse(userId));

                if (notification == null)
                {
                    return NotFound(new ErrorResponse { Message = "Notification not found" });
                }

                notification.Status = NotificationStatus.Delivered;
                notification.DeliveredAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification as read");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }
    }

    public class UpdatePatientRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? SmsOptIn { get; set; }
    }
}