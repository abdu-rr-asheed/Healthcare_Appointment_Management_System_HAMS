using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Services.Interfaces;
using System.Security.Claims;
using HAMS.API.Models.Entities;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            INotificationService notificationService,
            ApplicationDbContext context,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("slots")]
        [ProducesResponseType(typeof(IEnumerable<AvailableSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] Guid departmentId,
            [FromQuery] Guid? clinicianId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var slots = await _appointmentService.GetAvailableSlotsAsync(departmentId, clinicianId, startDate, endDate);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available slots");
                return BadRequest(new ErrorResponse { Message = "Failed to retrieve available slots" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointment = await _appointmentService.BookAppointmentAsync(userId, request);

                await _notificationService.SendBookingConfirmationAsync(appointment.Id);

                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Booking failed - unauthorized");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("Slot not available"))
            {
                _logger.LogWarning(ex, "Booking failed - slot not available");
                return Conflict(new ErrorResponse { Message = "Slot is no longer available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking failed");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAppointment(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointment = await _appointmentService.GetAppointmentByIdAsync(id, userId);
                if (appointment == null)
                {
                    return NotFound(new ErrorResponse { Message = "Appointment not found" });
                }

                return Ok(appointment);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get appointment");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingAppointments()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(userId);
                return Ok(appointments);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get upcoming appointments");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppointmentHistory(
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointments = await _appointmentService.GetAppointmentHistoryAsync(userId, status, startDate, endDate);
                return Ok(appointments);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get appointment history");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPut("{id}/reschedule")]
        [Authorize(Roles = "Patient,Administrator")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointment = await _appointmentService.RescheduleAppointmentAsync(userId, id, request);

                await _notificationService.SendRescheduleNotificationAsync(id, appointment.StartDateTime);

                return Ok(appointment);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Reschedule failed - unauthorized");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reschedule failed");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Patient,Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var result = await _appointmentService.CancelAppointmentAsync(userId, id, request);

                if (result)
                {
                    await _notificationService.SendCancellationConfirmationAsync(id);
                }

                return Ok(new { message = "Appointment cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Cancellation failed - unauthorized");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancellation failed");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpPost("{id}/dna")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkAsDidNotAttend(Guid id, [FromBody] DnaRequest request)
        {
            try
            {
                await _appointmentService.MarkAsDidNotAttendAsync(id, request.Reason);
                return Ok(new { message = "Appointment marked as Did Not Attend" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark appointment as DNA");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpGet("{id}/alternativeslots")]
        [ProducesResponseType(typeof(IEnumerable<AvailableSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAlternativeSlots(Guid id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound(new ErrorResponse { Message = "Appointment not found" });
                }

                var slot = appointment.Slot;
                var queryStartDate = startDate ?? DateTime.UtcNow;
                var queryEndDate = endDate ?? DateTime.UtcNow.AddDays(30);

                var alternativeSlots = await _context.AvailabilitySlots
                    .Include(s => s.Clinician)
                    .Include(s => s.Clinician.User)
                    .Include(s => s.Department)
                    .Where(s => s.ClinicianId == slot.ClinicianId
                        && s.Id != slot.Id
                        && s.IsAvailable
                        && !s.IsCancelled
                        && s.StartDateTime >= queryStartDate
                        && s.EndDateTime <= queryEndDate)
                    .OrderBy(s => s.StartDateTime)
                    .Take(10)
                    .ToListAsync();

                var result = alternativeSlots.Select(s => new AvailableSlotDto
                {
                    Id = s.Id,
                    StartDateTime = s.StartDateTime,
                    EndDateTime = s.EndDateTime,
                    ClinicianId = s.ClinicianId,
                    ClinicianName = $"{s.Clinician.User.FirstName} {s.Clinician.User.LastName}",
                    DepartmentId = s.DepartmentId,
                    DepartmentName = s.Department.Name,
                    IsAvailable = s.IsAvailable
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alternative slots");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("{id}/clinical-notes")]
        [ProducesResponseType(typeof(IEnumerable<ClinicalNoteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClinicalNotes(Guid id)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound(new ErrorResponse { Message = "Appointment not found" });
                }

                var notes = await _context.ClinicalNotes
                    .Include(n => n.Clinician)
                    .Include(n => n.Clinician.User)
                    .Where(n => n.AppointmentId == id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                var result = notes.Select(n => new ClinicalNoteResponseDto
                {
                    Id = n.Id,
                    AppointmentId = n.AppointmentId,
                    ClinicianId = n.ClinicianId,
                    ClinicianName = $"{n.Clinician.User.FirstName} {n.Clinician.User.LastName}",
                    Content = n.Content,
                    ConsultationType = n.ConsultationType,
                    Findings = n.Findings,
                    Recommendations = n.Recommendations,
                    IsPrivate = n.IsPrivate,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt,
                    SyncedToEhr = n.SyncedToEhr,
                    SyncedAt = n.SyncedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinical notes");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPost("{id}/clinical-notes")]
        [Authorize(Roles = "Clinician")]
        [ProducesResponseType(typeof(ClinicalNoteResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateClinicalNote(Guid id, [FromBody] CreateClinicalNoteRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound(new ErrorResponse { Message = "Appointment not found" });
                }

                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                if (clinician == null)
                {
                    return Forbid();
                }

                var note = new ClinicalNote
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = id,
                    ClinicianId = clinician.Id,
                    Content = request.Content,
                    ConsultationType = request.ConsultationType,
                    Findings = request.Findings,
                    Recommendations = request.Recommendations,
                    IsPrivate = request.IsPrivate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SyncedToEhr = false
                };

                _context.ClinicalNotes.Add(note);
                await _context.SaveChangesAsync();

                var clinicianUser = await _context.Users.FindAsync(Guid.Parse(userId));
                var result = new ClinicalNoteResponseDto
                {
                    Id = note.Id,
                    AppointmentId = note.AppointmentId,
                    ClinicianId = note.ClinicianId,
                    ClinicianName = $"{clinicianUser!.FirstName} {clinicianUser.LastName}",
                    Content = note.Content,
                    ConsultationType = note.ConsultationType ?? string.Empty,
                    Findings = note.Findings,
                    Recommendations = note.Recommendations,
                    IsPrivate = note.IsPrivate,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                    SyncedToEhr = note.SyncedToEhr,
                    SyncedAt = note.SyncedAt
                };

                return CreatedAtAction(nameof(GetClinicalNotes), new { id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create clinical note");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpPut("{id}/clinical-notes/{noteId}")]
        [Authorize(Roles = "Clinician")]
        [ProducesResponseType(typeof(ClinicalNoteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClinicalNote(Guid id, Guid noteId, [FromBody] UpdateClinicalNoteRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var clinician = await _context.Clinicians
                    .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                if (clinician == null)
                {
                    return Forbid();
                }

                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.ClinicianId == clinician.Id);

                if (note == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinical note not found" });
                }

                note.Content = request.Content;
                note.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var clinicianUser = await _context.Users.FindAsync(Guid.Parse(userId));
                var result = new ClinicalNoteResponseDto
                {
                    Id = note.Id,
                    AppointmentId = note.AppointmentId,
                    ClinicianId = note.ClinicianId,
                    ClinicianName = $"{clinicianUser!.FirstName} {clinicianUser.LastName}",
                    Content = note.Content,
                    ConsultationType = note.ConsultationType ?? string.Empty,
                    Findings = note.Findings,
                    Recommendations = note.Recommendations,
                    IsPrivate = note.IsPrivate,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                    SyncedToEhr = note.SyncedToEhr,
                    SyncedAt = note.SyncedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update clinical note");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpDelete("{id}/clinical-notes/{noteId}")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClinicalNote(Guid id, Guid noteId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                // Retrieve the note that belongs to this appointment
                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.AppointmentId == id);

                if (note == null)
                    return NotFound(new ErrorResponse { Message = "Clinical note not found" });

                // Non-admin callers may only delete their own notes
                if (!User.IsInRole("Administrator"))
                {
                    var clinician = await _context.Clinicians
                        .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                    if (clinician == null || note.ClinicianId != clinician.Id)
                        return Forbid();
                }

                _context.ClinicalNotes.Remove(note);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Clinical note deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete clinical note");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpPost("{id}/clinical-notes/{noteId}/sync-to-ehr")]
        [Authorize(Roles = "Clinician,Administrator")]
        [ProducesResponseType(typeof(SyncToEhrResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncNoteToEhr(Guid id, Guid noteId)
        {
            try
            {
                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.AppointmentId == id);

                if (note == null)
                {
                    return NotFound(new ErrorResponse { Message = "Clinical note not found" });
                }

                note.SyncedToEhr = true;
                note.SyncedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new SyncToEhrResponse
                {
                    Success = true,
                    EhrResourceId = $"ehr-note-{note.Id}",
                    SyncedAt = note.SyncedAt.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync clinical note to EHR");
                return StatusCode(500, new SyncToEhrResponse
                {
                    Success = false,
                    Error = "Failed to sync clinical note to EHR"
                });
            }
        }

        [HttpPost("suggestions")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(SlotSuggestionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSlotSuggestions([FromBody] GetSlotSuggestionsRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                var queryStartDate = request.Preferences.StartDate ?? DateTime.UtcNow;
                var queryEndDate = request.Preferences.EndDate ?? DateTime.UtcNow.AddDays(14);

                var slots = await _context.AvailabilitySlots
                    .Include(s => s.Clinician)
                    .Include(s => s.Clinician.User)
                    .Include(s => s.Department)
                    .Where(s => s.DepartmentId == request.DepartmentId
                        && s.IsAvailable
                        && !s.IsCancelled
                        && s.StartDateTime >= queryStartDate
                        && s.EndDateTime <= queryEndDate)
                    .Where(s => request.ClinicianId == null || s.ClinicianId == request.ClinicianId)
                    .ToListAsync();

                var rankedSlots = slots
                    .Select(s => new RankedSlotDto
                    {
                        Slot = new AvailableSlotDto
                        {
                            Id = s.Id,
                            StartDateTime = s.StartDateTime,
                            EndDateTime = s.EndDateTime,
                            ClinicianId = s.ClinicianId,
                            ClinicianName = $"{s.Clinician.User.FirstName} {s.Clinician.User.LastName}",
                            DepartmentId = s.DepartmentId,
                            DepartmentName = s.Department.Name,
                            IsAvailable = s.IsAvailable
                        },
                        Rank = 0,
                        Score = CalculateSlotScore(s, request.Preferences),
                        MatchReasons = GetMatchReasons(s, request.Preferences)
                    })
                    .OrderByDescending(s => s.Score)
                    .Take(3)
                    .ToList();

                for (int i = 0; i < rankedSlots.Count; i++)
                {
                    rankedSlots[i].Rank = i + 1;
                }

                return Ok(new SlotSuggestionsResponse
                {
                    Suggestions = rankedSlots,
                    TotalAvailable = slots.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get slot suggestions");
                return BadRequest(new ErrorResponse { Message = "Failed to get suggestions" });
            }
        }

        private double CalculateSlotScore(AvailabilitySlot slot, SlotPreferencesDto preferences)
        {
            double score = 50;
            var hour = slot.StartDateTime.Hour;

            if (preferences.PreferredTimeOfDay == "Morning" && hour >= 9 && hour < 12)
                score += 25;
            else if (preferences.PreferredTimeOfDay == "Afternoon" && hour >= 14 && hour < 17)
                score += 25;
            else if (preferences.PreferredTimeOfDay == "Evening" && (hour >= 17 || hour < 9))
                score += 25;
            else if (preferences.PreferredTimeOfDay == "Any")
                score += 15;

            var dayOfWeek = (int)slot.StartDateTime.DayOfWeek;
            if (preferences.PreferredDays != null && preferences.PreferredDays.Contains(dayOfWeek))
                score += 25;

            return score;
        }

        private List<string> GetMatchReasons(AvailabilitySlot slot, SlotPreferencesDto preferences)
        {
            var reasons = new List<string>();
            var hour = slot.StartDateTime.Hour;

            if (preferences.PreferredTimeOfDay == "Morning" && hour >= 9 && hour < 12)
                reasons.Add("Preferred morning time");
            else if (preferences.PreferredTimeOfDay == "Afternoon" && hour >= 14 && hour < 17)
                reasons.Add("Preferred afternoon time");
            else if (preferences.PreferredTimeOfDay == "Evening" && (hour >= 17 || hour < 9))
                reasons.Add("Preferred evening time");

            var dayOfWeek = (int)slot.StartDateTime.DayOfWeek;
            if (preferences.PreferredDays != null && preferences.PreferredDays.Contains(dayOfWeek))
                reasons.Add("Preferred day of week");

            if (reasons.Count == 0)
                reasons.Add("Available slot");

            return reasons;
        }
    }

    public class DnaRequest
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateClinicalNoteRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        public string? ConsultationType { get; set; }
        public string? Findings { get; set; }
        public string? Recommendations { get; set; }
        public bool IsPrivate { get; set; } = false;
    }

    public class UpdateClinicalNoteRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class GetSlotSuggestionsRequest
    {
        [Required]
        public Guid DepartmentId { get; set; }

        public Guid? ClinicianId { get; set; }
        public SlotPreferencesDto Preferences { get; set; } = new();
    }

    public class SlotPreferencesDto
    {
        public string? PreferredTimeOfDay { get; set; }
        public int[]? PreferredDays { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class RankedSlotDto
    {
        public AvailableSlotDto Slot { get; set; } = new();
        public int Rank { get; set; }
        public double Score { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }

    public class SlotSuggestionsResponse
    {
        public List<RankedSlotDto> Suggestions { get; set; } = new();
        public int TotalAvailable { get; set; }
    }

    public class SyncToEhrResponse
    {
        public bool Success { get; set; }
        public string? EhrResourceId { get; set; }
        public DateTime SyncedAt { get; set; }
        public string? Error { get; set; }
    }

    public class ClinicalNoteResponseDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid ClinicianId { get; set; }
        public string ClinicianName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ConsultationType { get; set; }
        public string? Findings { get; set; }
        public string? Recommendations { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool SyncedToEhr { get; set; }
        public DateTime? SyncedAt { get; set; }
    }
}