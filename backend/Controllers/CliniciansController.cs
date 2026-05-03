using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;
using System.Security.Claims;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class CliniciansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IClinicianService _clinicianService;
        private readonly ILogger<CliniciansController> _logger;

        public CliniciansController(
            ApplicationDbContext context,
            IClinicianService clinicianService,
            ILogger<CliniciansController> logger)
        {
            _context = context;
            _clinicianService = clinicianService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(typeof(IEnumerable<ClinicianListItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClinicians(
            [FromQuery] Guid? departmentId,
            [FromQuery] string? specialty,
            [FromQuery] bool? availableOnly)
        {
            try
            {
                var query = _context.Clinicians
                    .Include(c => c.User)
                    .Include(c => c.Department)
                    .Where(c => c.Status == ClinicianStatus.Active)
                    .AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(c => c.DepartmentId == departmentId.Value);

                if (!string.IsNullOrEmpty(specialty))
                    query = query.Where(c => c.Specialty.Contains(specialty));

                var clinicians = await query
                    .OrderBy(c => c.User.LastName)
                    .Select(c => new ClinicianListItemDto
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        FirstName = c.User.FirstName,
                        LastName = c.User.LastName,
                        Specialty = c.Specialty,
                        DepartmentId = c.DepartmentId,
                        DepartmentName = c.Department != null ? c.Department.Name : string.Empty,
                        LicenseNumber = c.LicenseNumber,
                        Status = c.Status.ToString()
                    })
                    .ToListAsync();

                return Ok(clinicians);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinicians list");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(ClinicianProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentClinician()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var clinician = await _context.Clinicians
                    .Include(c => c.User)
                    .Include(c => c.Department)
                    .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                if (clinician == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });
                }

                var result = new ClinicianProfileDto
                {
                    Id = clinician.Id,
                    FirstName = clinician.User.FirstName,
                    LastName = clinician.User.LastName,
                    Email = clinician.User.Email,
                    PhoneNumber = clinician.User.PhoneNumber ?? string.Empty,
                    Specialty = clinician.Specialty,
                    DepartmentName = clinician.Department?.Name ?? string.Empty,
                    LicenseNumber = clinician.LicenseNumber,
                    Qualifications = clinician.Qualifications ?? new List<string>(),
                    Status = clinician.Status.ToString()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinician profile");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(ClinicianProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateCurrentClinician([FromBody] UpdateClinicianRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var clinician = await _context.Clinicians
                    .Include(c => c.User)
                    .Include(c => c.Department)
                    .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                if (clinician == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });
                }

                var user = await _context.Users.FindAsync(Guid.Parse(userId));
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

                if (!string.IsNullOrEmpty(request.Specialty))
                {
                    clinician.Specialty = request.Specialty;
                }

                await _context.SaveChangesAsync();

                var result = new ClinicianProfileDto
                {
                    Id = clinician.Id,
                    FirstName = clinician.User.FirstName,
                    LastName = clinician.User.LastName,
                    Email = clinician.User.Email,
                    PhoneNumber = clinician.User.PhoneNumber ?? string.Empty,
                    Specialty = clinician.Specialty,
                    DepartmentName = clinician.Department?.Name ?? string.Empty,
                    LicenseNumber = clinician.LicenseNumber,
                    Qualifications = clinician.Qualifications ?? new List<string>(),
                    Status = clinician.Status.ToString()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update clinician profile");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("{id}/availability")]
        [ProducesResponseType(typeof(ClinicianAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClinicianAvailability(Guid id)
        {
            try
            {
                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clinician == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });
                }

                var result = await _clinicianService.GetAvailabilityAsync(clinician.UserId.ToString());
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinician availability");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPut("{id}/availability")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(typeof(ClinicianAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClinicianAvailability(Guid id, [FromBody] UpdateAvailabilityRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clinician == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });
                }

                // Clinicians can only update their own availability; Admins can update any
                if (clinician.UserId != Guid.Parse(userId) && !User.IsInRole("Administrator"))
                {
                    return Forbid();
                }

                await _clinicianService.UpdateAvailabilityAsync(clinician.UserId.ToString(), request);

                var result = await _clinicianService.GetAvailabilityAsync(clinician.UserId.ToString());
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update clinician availability");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpDelete("{id}/availability/leave/{leaveId}")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveLeavePeriod(Guid id, Guid leaveId)
        {
            try
            {
                var callerUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (callerUserId == null)
                    return Unauthorized();

                var isAdmin = User.IsInRole("Administrator");
                await _clinicianService.RemoveLeavePeriodAsync(id, leaveId, callerUserId, isAdmin);
                return Ok(new { message = "Leave period removed successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized leave period deletion attempt");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove leave period");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPost("{id}/slots/generate")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(typeof(GenerateSlotsResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateSlots(Guid id, [FromBody] GenerateSlotsRequestDto request)
        {
            try
            {
                var callerUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (callerUserId == null)
                    return Unauthorized();

                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clinician == null)
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });

                // Clinicians may only generate slots for themselves; Admins may target any.
                if (clinician.UserId != Guid.Parse(callerUserId) && !User.IsInRole("Administrator"))
                    return Forbid();

                var result = await _clinicianService.GenerateSlotsAsync(
                    clinician.UserId.ToString(), request);

                return Created($"api/clinicians/{id}/availability", result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate slots for clinician {ClinicianId}", id);
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("{id}/schedule")]
        [ProducesResponseType(typeof(ScheduleResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClinicianSchedule(Guid id, [FromQuery] DateTime? startDate, [FromQuery] string viewType = "daily")
        {
            try
            {
                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (clinician == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinician not found" });
                }

                var queryStartDate = startDate ?? DateTime.Today;
                var queryEndDate = viewType == "weekly"
                    ? queryStartDate.AddDays(7)
                    : queryStartDate.AddDays(1);

                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .Where(a => a.Slot!.ClinicianId == id
                        && a.Slot.StartDateTime >= queryStartDate
                        && a.Slot.EndDateTime <= queryEndDate
                        && a.Status != AppointmentStatus.Cancelled)
                    .ToListAsync();

                var result = new ScheduleResponseDto
                {
                    ClinicianId = id.ToString(),
                    ViewType = viewType,
                    DateRange = new DateRangeDto
                    {
                        Start = queryStartDate,
                        End = queryEndDate
                    },
                    Appointments = appointments.Select(a => new ScheduledAppointmentDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                        PatientNhsNumber = a.Patient.User.NhsNumber ?? string.Empty,
                        StartDateTime = a.Slot.StartDateTime,
                        EndDateTime = a.Slot.EndDateTime,
                        AppointmentType = a.Type.ToString(),
                        Status = a.Status.ToString(),
                        EhrFlags = new List<EhrFlagDto>(),
                        HasClinicalNotes = false,
                        IsFollowUpRequired = false
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinician schedule");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }
    }

    public class UpdateClinicianRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Specialty { get; set; }
    }

    public class UpdateAvailabilityRequest
    {
        public List<RegularScheduleDto>? RegularSchedule { get; set; }
        public List<LeavePeriodDto>? LeavePeriods { get; set; }
        public List<SlotConfigurationDto>? SlotConfigurations { get; set; }
    }

}