using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string NhsNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string UserName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public Guid? ClinicianId { get; set; }

        public bool TwoFactorEnabled { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public UserRole Role { get; set; }

        public DateTime? LockedUntil { get; set; }

        public int AccessFailedCount { get; set; }

        public virtual Patient? Patient { get; set; }

        public virtual Clinician? Clinician { get; set; }
    }

    public enum UserRole
    {
        Patient,
        Clinician,
        Administrator
    }
}