using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class ClinicalNote
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AppointmentId { get; set; }

        [Required]
        public Appointment Appointment { get; set; } = null!;

        [Required]
        public Guid ClinicianId { get; set; }

        [Required]
        public Clinician Clinician { get; set; } = null!;

        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = false;

        [MaxLength(100)]
        public string ConsultationType { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Findings { get; set; }

        [MaxLength(5000)]
        public string? Recommendations { get; set; }

        public bool SyncedToEhr { get; set; } = false;

        public DateTime? SyncedAt { get; set; }

        public string? EhrResourceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}