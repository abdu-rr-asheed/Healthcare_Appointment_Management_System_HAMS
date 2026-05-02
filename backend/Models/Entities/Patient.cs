using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class Patient
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public User User { get; set; } = null!;

        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Postcode { get; set; } = string.Empty;

        public string NhsNumber { get; set; } = string.Empty;

        public bool SmsOptIn { get; set; } = true;

        public string? MedicalNotes { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        [MaxLength(500)]
        public string? ProfileImageUrl { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}