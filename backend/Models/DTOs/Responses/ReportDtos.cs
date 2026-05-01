namespace HAMS.API.Models.DTOs.Responses;

public class AppointmentSummaryDto
{
    public int TotalAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int DidNotAttend { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class ClinicianPerformanceDto
{
    public string ClinicianId { get; set; } = string.Empty;
    public string ClinicianName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int DidNotAttend { get; set; }
    public double CompletionRate { get; set; }
    public double CancellationRate { get; set; }
    public double DnaRate { get; set; }
    public double AverageRating { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class DashboardStatsDto
{
    public int TotalPatients { get; set; }
    public int TotalClinicians { get; set; }
    public int TodayAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int PendingRequests { get; set; }
    public double DailyUtilisationRate { get; set; }
    public double WeeklyUtilisationRate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ReportExportDto
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSize { get; set; } = string.Empty;
}