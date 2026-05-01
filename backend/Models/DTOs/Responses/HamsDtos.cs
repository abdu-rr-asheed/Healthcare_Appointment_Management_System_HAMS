namespace HAMS.API.Models.DTOs.Responses
{
    public class PatientProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NhsNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public AddressDto Address { get; set; } = new();
        public string City { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;
        public bool SmsOptIn { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsActive { get; set; }
        public string? GpPractice { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalConditions { get; set; }
        public string? CurrentMedications { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class AddressDto
    {
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;
    }

    public class PatientNotificationDto
    {
        public Guid NotificationId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public Guid AppointmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid Id { get; set; }
    }

    public class ClinicianProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ClinicianId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string GmcNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public List<string> Qualifications { get; set; } = new();
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public List<RegularScheduleDto> RegularSchedule { get; set; } = new();
        public List<LeavePeriodDto> LeavePeriods { get; set; } = new();
        public List<SlotConfigurationDto> SlotConfigurations { get; set; } = new();
    }

    public class ClinicianAvailabilityDto
    {
        public Guid ClinicianId { get; set; }
        public List<RegularScheduleDto> RegularSchedule { get; set; } = new();
        public List<LeavePeriodDto> LeavePeriods { get; set; } = new();
        public List<SlotConfigurationDto> SlotConfigurations { get; set; } = new();
    }

    public class RegularScheduleDto
    {
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool Recurring { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class LeavePeriodDto
    {
        public Guid LeavePeriodId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = true;
    }

    public class SlotConfigurationDto
    {
        public string AppointmentType { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int BufferMinutes { get; set; }
    }

    public class PaginatedResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class GenerateSlotsRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> AppointmentTypes { get; set; } = new();
    }

    public class GenerateSlotsResponseDto
    {
        public int SlotsGenerated { get; set; }
        public int SlotsBlocked { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class ScheduleResponseDto
    {
        public string ClinicianId { get; set; } = string.Empty;
        public string ViewType { get; set; } = string.Empty;
        public DateRangeDto DateRange { get; set; } = new();
        public List<ScheduledAppointmentDto> Appointments { get; set; } = new();
    }

    public class ScheduleAppointmentDto
    {
        public Guid SlotId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ScheduledAppointmentDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNhsNumber { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string AppointmentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public List<EhrFlagDto> EhrFlags { get; set; } = new();
        public bool HasClinicalNotes { get; set; }
        public bool IsFollowUpRequired { get; set; }
        public Guid? AppointmentSlotId { get; set; }
        public Guid SlotId { get; set; }
        public string? SlotStatus { get; set; }
        public string? SlotType { get; set; }
    }

    public class DateRangeDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class EhrFlagDto
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GenerateReportRequestDto
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ReportFiltersDto? Filters { get; set; }
        public string Format { get; set; } = "CSV";
        public bool IncludeCharts { get; set; }
    }

    public class ReportFiltersDto
    {
        public List<string>? DepartmentIds { get; set; }
        public List<string>? ClinicianIds { get; set; }
        public List<string>? AppointmentTypes { get; set; }
        public List<string>? Status { get; set; }
    }

    public class GenerateReportResponseDto
    {
        public Guid ReportId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ReportDataDto
    {
        public ReportSummaryDto Summary { get; set; } = new();
        public List<ReportDetailDto> Details { get; set; } = new();
    }

    public class ReportSummaryDto
    {
        public int TotalBookings { get; set; }
        public int TotalCancellations { get; set; }
        public int TotalDna { get; set; }
        public double AverageUtilisation { get; set; }
    }

    public class ReportDetailDto
    {
        public DateTime Date { get; set; }
        public string Department { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public int Cancellations { get; set; }
    }

    public class AuditLogEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public object? Details { get; set; }
        public string Outcome { get; set; } = string.Empty;
    }

    public class AuditLogResponse
    {
        public List<AuditLogEntryDto> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class AuditLogQuery
    {
        public string? UserId { get; set; }
        public string? ActionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AppointmentHistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConfirmationReference { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string ClinicianName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}