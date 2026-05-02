using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Models.DTOs.Requests;

public class AppointmentRequest
{
    public required Guid PatientId { get; set; }
    public required Guid ClinicianId { get; set; }
    public required DateTime AppointmentDate { get; set; }
    public required string AppointmentType { get; set; }
    public string? Reason { get; set; }
}

public class CancellationRequest
{
    public required Guid AppointmentId { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsPatientCancellation { get; set; }
}

public class ClinicalNoteRequest
{
    public required Guid AppointmentId { get; set; }
    public required string NoteContent { get; set; }
    public required string NoteType { get; set; }
}

public class PatientRegistrationRequest
{
    public required string NhsNumber { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required DateTime DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
}

public class ClinicianRegistrationRequest
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Specialty { get; set; }
    public string? JobTitle { get; set; }
    public required string GmcNumber { get; set; }
}

public class UpdatePatientRequest
{
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
}

public class GenerateSlotsRequest
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public List<string> AppointmentTypes { get; set; } = new();
}

public class UpdateClinicianProfileRequestDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Specialty { get; set; }
}

public class UpdateAvailabilityRequestDto
{
    public List<RegularScheduleDto>? RegularSchedule { get; set; }
    public List<LeavePeriodDto>? LeavePeriods { get; set; }
    public List<SlotConfigurationDto>? SlotConfigurations { get; set; }
}

public class GenerateSlotsRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> AppointmentTypes { get; set; } = new();
}

public class ReportRequest
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required string ReportType { get; set; }
}