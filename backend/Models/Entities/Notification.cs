using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.Entities
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public User User { get; set; } = null!;

        public Guid? PatientId { get; set; }

        public Guid? AppointmentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        public bool IsRead { get; set; } = false;

        public NotificationChannel Channel { get; set; }

        [MaxLength(500)]
        public string? Recipient { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? FailedAt { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Delivered,
        Failed
    }

    public enum NotificationType
    {
        AppointmentReminder,
        AppointmentConfirmation,
        AppointmentCancellation,
        AppointmentReschedule,
        ClinicalNoteUpdate,
        SystemNotification
    }

    public enum NotificationChannel
    {
        Email,
        Sms,
        InApp
    }
}