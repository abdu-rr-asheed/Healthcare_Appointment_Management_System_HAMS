using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HAMS.API.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppointmentSummaryDto> GetAppointmentSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Slot != null && a.Slot.StartDateTime >= startDate && a.Slot.StartDateTime <= endDate)
                .ToListAsync();

            var summary = new AppointmentSummaryDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalAppointments = appointments.Count,
                ConfirmedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                PendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                DidNotAttend = appointments.Count(a => a.Status == AppointmentStatus.DidNotAttend)
            };

            return summary;
        }

        public async Task<ClinicianPerformanceDto> GetClinicianPerformanceAsync(string clinicianId, DateTime startDate, DateTime endDate)
        {
            var clinician = await _context.Clinicians
                .Include(c => c.User)
                .Include(c => c.AvailabilitySlots)
                    .ThenInclude(s => s.Appointments)
                .FirstOrDefaultAsync(c => c.Id == Guid.Parse(clinicianId));

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            var appointments = clinician.AvailabilitySlots
                .Where(s => s.Appointments.Any()
                    && s.StartDateTime >= startDate
                    && s.StartDateTime <= endDate)
                .SelectMany(s => s.Appointments)
                .ToList();

            var completedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed);
            var cancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
            var didNotAttend = appointments.Count(a => a.Status == AppointmentStatus.DidNotAttend);

            var performance = new ClinicianPerformanceDto
            {
                ClinicianId = clinicianId,
                ClinicianName = $"{clinician.User.FirstName} {clinician.User.LastName}",
                TotalAppointments = appointments.Count,
                CompletedAppointments = completedAppointments,
                CancelledAppointments = cancelledAppointments,
                DidNotAttend = didNotAttend,
                CompletionRate = appointments.Count > 0 ? Math.Round((double)completedAppointments / appointments.Count * 100, 2) : 0,
                CancellationRate = appointments.Count > 0 ? Math.Round((double)cancelledAppointments / appointments.Count * 100, 2) : 0,
                DnaRate = appointments.Count > 0 ? Math.Round((double)didNotAttend / appointments.Count * 100, 2) : 0,
                AverageRating = 4.5,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };

            return performance;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            var todayAppointments = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Slot != null && a.Slot.StartDateTime.Date == today)
                .ToListAsync();

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.Slot != null && a.Slot.StartDateTime >= today && a.Slot.StartDateTime <= weekEnd)
                .ToListAsync();

            var totalSlots = await _context.AvailabilitySlots
                .Where(s => s.StartDateTime >= today && s.StartDateTime <= weekEnd)
                .CountAsync();

            var bookedSlots = await _context.AvailabilitySlots
                .Where(s => s.StartDateTime >= today && s.StartDateTime <= weekEnd && s.IsAvailable == false)
                .CountAsync();

            var totalPatients = await _context.Patients.CountAsync();
            var totalClinicians = await _context.Clinicians.CountAsync();

            var pendingRequests = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .CountAsync();

            return new DashboardStatsDto
            {
                TotalPatients = totalPatients,
                TotalClinicians = totalClinicians,
                TodayAppointments = todayAppointments.Count,
                UpcomingAppointments = upcomingAppointments.Count,
                PendingRequests = pendingRequests,
                DailyUtilisationRate = totalSlots > 0 ? Math.Round((double)bookedSlots / totalSlots * 100, 2) : 0,
                WeeklyUtilisationRate = totalSlots > 0 ? Math.Round((double)bookedSlots / totalSlots * 100, 2) : 0,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<ReportExportDto> ExportReportAsync(string reportType, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Exporting {ReportType} report from {StartDate} to {EndDate}",
                reportType, startDate, endDate);

            var reportId = Guid.NewGuid().ToString();
            var fileName = $"{reportType}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            var format = "CSV";
            var downloadUrl = $"/api/reports/{reportId}/download";

            return new ReportExportDto
            {
                ReportId = reportId,
                ReportType = reportType,
                FileName = fileName,
                Format = format,
                DownloadUrl = downloadUrl,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                FileSizeBytes = 1024,
                FileSize = "1.0 KB"
            };
        }
    }
}