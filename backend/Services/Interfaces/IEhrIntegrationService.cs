namespace HAMS.API.Services.Interfaces;

public interface IEhrIntegrationService
{
    Task<PatientDemographicsDto?> GetPatientByNhsNumberAsync(string nhsNumber);
    Task<MedicalHistoryDto?> GetMedicalHistoryAsync(string nhsNumber);
    Task<AllergiesDto?> GetAllergiesAsync(string nhsNumber);
    Task<MedicationsDto?> GetMedicationsAsync(string nhsNumber);
    Task<SyncResultDto> SyncPatientDataAsync(string nhsNumber);
    Task<HealthCheckDto> CheckServiceHealthAsync();
}

public class PatientDemographicsDto
{
    public string Id { get; set; } = string.Empty;
    public string NhsNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class MedicalHistoryDto
{
    public string PatientId { get; set; } = string.Empty;
    public string NhsNumber { get; set; } = string.Empty;
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
    public List<ConditionDto> ChronicConditions { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
}

public class DiagnosisDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DateRecorded { get; set; }
    public string? Status { get; set; }
}

public class ConditionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? OnsetDate { get; set; }
    public string? Severity { get; set; }
}

public class AllergiesDto
{
    public string PatientId { get; set; } = string.Empty;
    public string NhsNumber { get; set; } = string.Empty;
    public List<AllergyDto> Allergies { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
}

public class AllergyDto
{
    public string Substance { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? Reaction { get; set; }
    public DateTime? OnsetDate { get; set; }
}

public class MedicationsDto
{
    public string PatientId { get; set; } = string.Empty;
    public string NhsNumber { get; set; } = string.Empty;
    public List<MedicationDto> Medications { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
}

public class MedicationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public DateTime? StartDate { get; set; }
    public string? PrescribedBy { get; set; }
    public bool IsActive { get; set; }
}

public class SyncResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public DateTime SyncedAt { get; set; }
}

public class HealthCheckDto
{
    public bool Healthy { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedAt { get; set; }
}