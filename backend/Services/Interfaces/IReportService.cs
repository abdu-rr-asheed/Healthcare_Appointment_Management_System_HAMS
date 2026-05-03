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
}
