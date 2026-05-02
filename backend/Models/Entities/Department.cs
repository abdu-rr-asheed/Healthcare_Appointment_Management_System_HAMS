using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class Department
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Clinician> Clinicians { get; set; } = new List<Clinician>();

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}