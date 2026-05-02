using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class RegularSchedule
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        [Required]
        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool Recurring { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}