namespace HAMS.API.Models.DTOs.Responses
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public string ConfirmationReference { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public PatientDto Patient { get; set; } = null!;
        public ClinicianDto Clinician { get; set; } = null!;
        public DepartmentDto Department { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class PatientDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string NhsNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
    }

    public class ClinicianDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public List<string> Qualifications { get; set; } = new List<string>();
        public string Status { get; set; } = string.Empty;
    }

    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AvailableSlotDto
    {
        public Guid Id { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public Guid ClinicianId { get; set; }
        public string ClinicianName { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}