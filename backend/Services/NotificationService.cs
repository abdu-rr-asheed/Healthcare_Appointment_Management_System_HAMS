using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;

namespace HAMS.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendEmailReminderAsync(Guid appointmentId, string reminderType)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new Exception("Appointment not found");
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = appointment.Patient.UserId,
                    Subject = $"Appointment Reminder - {reminderType}",
                    Message = $"Dear {appointment.Patient.User.FirstName}, this is a reminder about your appointment on {appointment.Slot.StartDateTime:dd MMM yyyy at HH:mm}.",
                    Type = reminderType,
                    Status = NotificationStatus.Sent,
                    Channel = NotificationChannel.Email,
                    Recipient = appointment.Patient.User.Email,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email reminder sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email reminder for appointment {appointmentId}");
                throw;
            }
        }

        public async Task SendSmsReminderAsync(Guid appointmentId, string reminderType)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new Exception("Appointment not found");
                }

                if (!appointment.Patient.SmsOptIn)
                {
                    return;
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = appointment.Patient.UserId,
                    Subject = $"Appointment Reminder - {reminderType}",
                    Message = $"Reminder: Your appointment is on {appointment.Slot.StartDateTime:dd MMM yyyy at HH:mm}.",
                    Type = reminderType,
                    Status = NotificationStatus.Sent,
                    Channel = NotificationChannel.Sms,
                    Recipient = appointment.Patient.User.PhoneNumber,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"SMS reminder sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS reminder for appointment {appointmentId}");
                throw;
            }
        }

        public async Task SendBookingConfirmationAsync(Guid appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new Exception("Appointment not found");
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = appointment.Patient.UserId,
                    Subject = "Appointment Booking Confirmation",
                    Message = $"Your appointment has been confirmed. Reference: {appointment.ConfirmationReference}. Date: {appointment.Slot.StartDateTime:dd MMM yyyy at HH:mm}.",
                    Type = "BookingConfirmation",
                    Status = NotificationStatus.Sent,
                    Channel = NotificationChannel.Email,
                    Recipient = appointment.Patient.User.Email,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Booking confirmation sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking confirmation for appointment {appointmentId}");
                throw;
            }
        }

        public async Task SendCancellationConfirmationAsync(Guid appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new Exception("Appointment not found");
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = appointment.Patient.UserId,
                    Subject = "Appointment Cancelled",
                    Message = $"Your appointment on {appointment.Slot.StartDateTime:dd MMM yyyy at HH:mm} has been cancelled. Reference: {appointment.ConfirmationReference}.",
                    Type = "Cancellation",
                    Status = NotificationStatus.Sent,
                    Channel = NotificationChannel.Email,
                    Recipient = appointment.Patient.User.Email,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cancellation confirmation sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send cancellation confirmation for appointment {appointmentId}");
                throw;
            }
        }

        public async Task SendRescheduleNotificationAsync(Guid appointmentId, DateTime newDateTime)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new Exception("Appointment not found");
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = appointment.Patient.UserId,
                    Subject = "Appointment Rescheduled",
                    Message = $"Your appointment has been rescheduled to {newDateTime:dd MMM yyyy at HH:mm}. New reference: {appointment.ConfirmationReference}.",
                    Type = "Reschedule",
                    Status = NotificationStatus.Sent,
                    Channel = NotificationChannel.Email,
                    Recipient = appointment.Patient.User.Email,
                    SentAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reschedule notification sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send reschedule notification for appointment {appointmentId}");
                throw;
            }
        }

        public async Task LogDeliveryFailureAsync(Guid notificationId, string reason)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Failed;
                notification.FailedAt = DateTime.UtcNow;
                notification.FailureReason = reason;
                await _context.SaveChangesAsync();

                _logger.LogError($"Notification delivery failed: {reason}");
            }
        }

        public async Task SendNotificationAsync(Guid userId, string subject, string message, string type, Guid? appointmentId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Subject = subject,
                Message = message,
                Type = type,
                Status = NotificationStatus.Pending,
                Channel = NotificationChannel.InApp,
                CreatedAt = DateTime.UtcNow,
                AppointmentId = appointmentId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}