using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class AvailabilitySlot
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        [Required]
        public Guid DepartmentId { get; set; }

        [Required]
        public Department Department { get; set; } = null!;

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }

        [Required]
        public bool IsAvailable { get; set; } = true;

        public bool IsCancelled { get; set; } = false;

        public string? CancellationReason { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}