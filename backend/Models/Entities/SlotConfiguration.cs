using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class SlotConfiguration
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string AppointmentType { get; set; } = string.Empty;

        [Required]
        public int DurationMinutes { get; set; }

        public int BufferMinutes { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}