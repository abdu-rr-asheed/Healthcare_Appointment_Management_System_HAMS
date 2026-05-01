using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class AppointmentSlot
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        public SlotStatus Status { get; set; } = SlotStatus.Available;

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        public Guid? AppointmentId { get; set; }

        public Appointment? Appointment { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum SlotStatus
    {
        Available,
        Booked,
        Cancelled,
        Blocked
    }
}