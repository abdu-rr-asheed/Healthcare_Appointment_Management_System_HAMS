using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HAMS.API.Models.Entities
{
    public class Appointment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ConfirmationReference { get; set; } = string.Empty;

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Patient Patient { get; set; } = null!;

        [Required]
        public Guid SlotId { get; set; }

        [Required]
        public AvailabilitySlot Slot { get; set; } = null!;

        [Required]
        public Department Department { get; set; } = null!;

        [Required]
        public Guid DepartmentId { get; set; }

        public AppointmentType Type { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        [MaxLength(500)]
        public string? RescheduleReason { get; set; }

        public DateTime? RescheduledAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? DidNotAttendAt { get; set; }

        [MaxLength(500)]
        public string? DnaReason { get; set; }

        public bool ReminderSent48Hour { get; set; } = false;

        public bool ReminderSent2Hour { get; set; } = false;

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        public virtual AppointmentSlot? AppointmentSlot { get; set; }
    }

    public enum AppointmentType
    {
        InitialConsultation,
        FollowUp,
        Emergency
    }

    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed,
        DidNotAttend
    }
}