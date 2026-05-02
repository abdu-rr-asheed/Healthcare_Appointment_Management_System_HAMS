using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class Clinician
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public User User { get; set; } = null!;

        [Required]
        public Guid DepartmentId { get; set; }

        [Required]
        public Department Department { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Specialty { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string JobTitle { get; set; } = string.Empty;

        [MaxLength(50)]
        public string GmcNumber { get; set; } = string.Empty;

        [Required]
        public Guid ClinicianId { get; set; }

        public List<string> Qualifications { get; set; } = new List<string>();

        public DateTime? StartDate { get; set; }

        public ClinicianStatus Status { get; set; } = ClinicianStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<AvailabilitySlot> AvailabilitySlots { get; set; } = new List<AvailabilitySlot>();

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();

        public virtual ICollection<RegularSchedule> RegularSchedules { get; set; } = new List<RegularSchedule>();

        public virtual ICollection<LeavePeriod> LeavePeriods { get; set; } = new List<LeavePeriod>();

        public virtual ICollection<SlotConfiguration> SlotConfigurations { get; set; } = new List<SlotConfiguration>();

        public virtual ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();
    }

    public enum ClinicianStatus
    {
        Active,
        Inactive,
        OnLeave
    }
}