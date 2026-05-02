using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Services.Interfaces
{
    public interface IReportService
    {
        Task<AppointmentSummaryDto> GetAppointmentSummaryAsync(DateTime startDate, DateTime endDate);
        Task<ClinicianPerformanceDto> GetClinicianPerformanceAsync(string clinicianId, DateTime startDate, DateTime endDate);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<ReportExportDto> ExportReportAsync(string reportType, DateTime startDate, DateTime endDate);
    }

    public class UpdatePatientRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}