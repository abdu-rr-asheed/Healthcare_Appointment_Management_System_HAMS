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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/dashboard/admin
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var todayEnd   = todayStart.AddDays(1);
                var weekStart  = todayStart.AddDays(-(int)todayStart.DayOfWeek + 1); // Monday
                var weekEnd    = weekStart.AddDays(7);

                // ── User stats ────────────────────────────────────────────
                var totalUsers    = await _context.Users.CountAsync();
                var patientCount  = await _context.Users.CountAsync(u => u.Role == UserRole.Patient);
                var clinicianCount= await _context.Users.CountAsync(u => u.Role == UserRole.Clinician);
                var adminCount    = await _context.Users.CountAsync(u => u.Role == UserRole.Administrator);
                var activeUsers   = await _context.Users.CountAsync(u => u.IsActive);
                var newUsersThisWeek = await _context.Users
                    .CountAsync(u => u.CreatedAt >= weekStart && u.CreatedAt < weekEnd);

                // ── Appointment stats ─────────────────────────────────────
                var allAppts    = await _context.Appointments.CountAsync();
                var todayAppts  = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => a.Slot.StartDateTime >= todayStart && a.Slot.StartDateTime < todayEnd);
                var weekAppts   = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => a.Slot.StartDateTime >= weekStart && a.Slot.StartDateTime < weekEnd);

                var pendingCount    = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
                var confirmedCount  = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed);
                var completedCount  = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed);
                var cancelledCount  = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled);
                var dnaCount        = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.DidNotAttend);

                // ── Appointment type breakdown ────────────────────────────
                var initialCount = await _context.Appointments.CountAsync(a => a.Type == AppointmentType.InitialConsultation);
                var followUpCount= await _context.Appointments.CountAsync(a => a.Type == AppointmentType.FollowUp);
                var emergencyCount= await _context.Appointments.CountAsync(a => a.Type == AppointmentType.Emergency);

                // ── Department stats ──────────────────────────────────────
                var totalDepts = await _context.Departments.CountAsync();
                var deptAppointments = await _context.Appointments
                    .Include(a => a.Department)
                    .GroupBy(a => new { a.DepartmentId, a.Department.Name })
                    .Select(g => new { g.Key.Name, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                // ── Recent audit activity ─────────────────────────────────
                var recentActivity = await _context.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(8)
                    .Select(a => new
                    {
                        a.Id,
                        a.Action,
                        a.UserName,
                        a.UserRole,
                        a.ResourceType,
                        a.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    userStats = new
                    {
                        totalUsers,
                        patients   = patientCount,
                        clinicians = clinicianCount,
                        admins     = adminCount,
                        activeUsers,
                        newUsersThisWeek
                    },
                    appointmentStats = new
                    {
                        total       = allAppts,
                        today       = todayAppts,
                        thisWeek    = weekAppts,
                        pending     = pendingCount,
                        confirmed   = confirmedCount,
                        completed   = completedCount,
                        cancelled   = cancelledCount,
                        didNotAttend= dnaCount,
                        typeBreakdown = new
                        {
                            initialConsultation = initialCount,
                            followUp            = followUpCount,
                            emergency           = emergencyCount
                        }
                    },
                    departmentStats = new
                    {
                        totalDepartments = totalDepts,
                        topDepartments   = deptAppointments
                    },
                    recentActivity,
                    systemStatus = new
                    {
                        databaseConnected = true,
                        generatedAt       = now
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin dashboard data");
                return StatusCode(500, new ErrorResponse { Message = "Failed to load dashboard data" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /api/dashboard/clinician
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("clinician")]
        [Authorize(Roles = "Clinician")]
        public async Task<IActionResult> GetClinicianDashboard()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new ErrorResponse { Message = "User not identified" });

                var clinician = await _context.Clinicians
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(userId));

                if (clinician == null)
                    return NotFound(new ErrorResponse { Message = "Clinician profile not found" });

                var now        = DateTime.UtcNow;
                var todayStart = now.Date;
                var todayEnd   = todayStart.AddDays(1);
                var weekStart  = todayStart.AddDays(-(int)todayStart.DayOfWeek + 1);
                var weekEnd    = weekStart.AddDays(7);
                var nextSevenDays = todayStart.AddDays(7);

                // ── Today's appointments ──────────────────────────────────
                var todayAppointments = await _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Department)
                    .Where(a => a.ClinicianId == clinician.Id
                             && a.Slot.StartDateTime >= todayStart
                             && a.Slot.StartDateTime < todayEnd
                             && a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.Slot.StartDateTime)
                    .Select(a => new
                    {
                        a.Id,
                        a.ConfirmationReference,
                        patientName    = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                        patientNhsNumber = a.Patient.User.NhsNumber,
                        startDateTime  = a.Slot.StartDateTime,
                        endDateTime    = a.Slot.EndDateTime,
                        appointmentType= a.Type.ToString(),
                        departmentName = a.Department.Name,
                        status         = a.Status.ToString(),
                        a.Notes
                    })
                    .ToListAsync();

                // ── Upcoming appointments (next 7 days, excluding today) ──
                var upcomingAppointments = await _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Department)
                    .Where(a => a.ClinicianId == clinician.Id
                             && a.Slot.StartDateTime >= todayEnd
                             && a.Slot.StartDateTime < nextSevenDays
                             && a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.Slot.StartDateTime)
                    .Select(a => new
                    {
                        a.Id,
                        a.ConfirmationReference,
                        patientName    = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                        startDateTime  = a.Slot.StartDateTime,
                        endDateTime    = a.Slot.EndDateTime,
                        appointmentType= a.Type.ToString(),
                        departmentName = a.Department.Name,
                        status         = a.Status.ToString()
                    })
                    .ToListAsync();

                // ── Quick stats ───────────────────────────────────────────
                var apptsTodayCount   = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => a.ClinicianId == clinician.Id
                                  && a.Slot.StartDateTime >= todayStart
                                  && a.Slot.StartDateTime < todayEnd
                                  && a.Status != AppointmentStatus.Cancelled);

                var apptsWeekCount    = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => a.ClinicianId == clinician.Id
                                  && a.Slot.StartDateTime >= weekStart
                                  && a.Slot.StartDateTime < weekEnd);

                var completedAllTime  = await _context.Appointments
                    .CountAsync(a => a.ClinicianId == clinician.Id
                                  && a.Status == AppointmentStatus.Completed);

                var pendingTodayCount = await _context.Appointments
                    .Include(a => a.Slot)
                    .CountAsync(a => a.ClinicianId == clinician.Id
                                  && a.Slot.StartDateTime >= todayStart
                                  && a.Slot.StartDateTime < todayEnd
                                  && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));

                var nextAppointment = await _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Where(a => a.ClinicianId == clinician.Id
                             && a.Slot.StartDateTime >= now
                             && a.Status == AppointmentStatus.Confirmed)
                    .OrderBy(a => a.Slot.StartDateTime)
                    .Select(a => new
                    {
                        a.Id,
                        patientName   = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                        startDateTime = a.Slot.StartDateTime
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    clinicianInfo = new
                    {
                        id            = clinician.Id,
                        name          = $"Dr. {clinician.User.FirstName} {clinician.User.LastName}",
                        specialty     = clinician.Specialty,
                        licenseNumber = clinician.LicenseNumber
                    },
                    todayAppointments,
                    upcomingAppointments,
                    stats = new
                    {
                        appointmentsToday    = apptsTodayCount,
                        appointmentsThisWeek = apptsWeekCount,
                        completedAllTime,
                        pendingToday         = pendingTodayCount,
                        upcomingNext7Days    = upcomingAppointments.Count
                    },
                    nextAppointment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching clinician dashboard data");
                return StatusCode(500, new ErrorResponse { Message = "Failed to load dashboard data" });
            }
        }
    }
}
