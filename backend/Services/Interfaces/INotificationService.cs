namespace HAMS.API.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailReminderAsync(Guid appointmentId, string reminderType);
        Task SendSmsReminderAsync(Guid appointmentId, string reminderType);
        Task SendBookingConfirmationAsync(Guid appointmentId);
        Task SendCancellationConfirmationAsync(Guid appointmentId);
        Task SendRescheduleNotificationAsync(Guid appointmentId, DateTime newDateTime);
        Task LogDeliveryFailureAsync(Guid notificationId, string reason);
        Task SendNotificationAsync(Guid userId, string subject, string message, string type, Guid? appointmentId = null);
    }
}