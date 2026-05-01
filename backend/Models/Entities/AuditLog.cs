using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? UserRole { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ResourceType { get; set; } = string.Empty;

        public Guid? ResourceId { get; set; }

        [MaxLength(50)]
        public string? Method { get; set; }

        [MaxLength(500)]
        public string? Resource { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public Dictionary<string, object>? Details { get; set; }

        [Required]
        [MaxLength(20)]
        public string Outcome { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum AuditAction
    {
        UserLogin,
        UserLogout,
        AppointmentBooked,
        AppointmentCancelled,
        AppointmentRescheduled,
        PatientDataAccessed,
        ClinicalNotesAdded,
        EhrDataAccessed,
        ReportGenerated,
        ConfigurationChanged
    }
}