using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HAMS.API.Jobs
{
    public class ReminderJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReminderJob> _logger;

        public ReminderJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<ReminderJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Send48HourRemindersAsync()
        {
            _logger.LogInformation("Starting 48-hour reminder job");

            var windowStart = DateTime.UtcNow.AddHours(47);
            var windowEnd = DateTime.UtcNow.AddHours(49);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Slot)
                .Include(a => a.Clinician)
                    .ThenInclude(c => c.User)
                .Where(a => a.Status == AppointmentStatus.Confirmed
                    && a.Slot.StartDateTime >= windowStart
                    && a.Slot.StartDateTime <= windowEnd
                    && !a.ReminderSent48Hour)
                .ToListAsync();

            var sentCount = 0;
            foreach (var appointment in appointments)
            {
                try
                {
                    var message = $"Reminder: Your appointment is in 48 hours on {appointment.Slot.StartDateTime:dddd, d MMMM yyyy} at {appointment.Slot.StartDateTime:HH:mm} with {appointment.Clinician.User.FirstName} {appointment.Clinician.User.LastName}.";

                    await _notificationService.SendNotificationAsync(
                        appointment.Patient.UserId,
                        "AppointmentReminder",
                        "Appointment Reminder",
                        message,
                        appointment.Id);

                    appointment.ReminderSent48Hour = true;
                    await _context.SaveChangesAsync();
                    sentCount++;

                    _logger.LogInformation(
                        "Sent 48-hour reminder for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id,
                        appointment.Patient.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send 48-hour reminder for appointment {AppointmentId}",
                        appointment.Id);
                }
            }

            _logger.LogInformation("Completed 48-hour reminder job. Sent {Count} reminders", sentCount);
        }

        public async Task Send2HourRemindersAsync()
        {
            _logger.LogInformation("Starting 2-hour reminder job");

            var windowStart = DateTime.UtcNow.AddHours(1.5);
            var windowEnd = DateTime.UtcNow.AddHours(2.5);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Slot)
                .Include(a => a.Clinician)
                    .ThenInclude(c => c.User)
                .Where(a => a.Status == AppointmentStatus.Confirmed
                    && a.Slot.StartDateTime >= windowStart
                    && a.Slot.StartDateTime <= windowEnd
                    && !a.ReminderSent2Hour)
                .ToListAsync();

            var sentCount = 0;
            foreach (var appointment in appointments)
            {
                try
                {
                    var message = $"Reminder: Your appointment is in 2 hours at {appointment.Slot.StartDateTime:HH:mm} today with {appointment.Clinician.User.FirstName} {appointment.Clinician.User.LastName}. Please arrive 10 minutes early.";

                    await _notificationService.SendNotificationAsync(
                        appointment.Patient.UserId,
                        "AppointmentReminder",
                        "Appointment Reminder - 2 Hours",
                        message,
                        appointment.Id);

                    appointment.ReminderSent2Hour = true;
                    await _context.SaveChangesAsync();
                    sentCount++;

                    _logger.LogInformation(
                        "Sent 2-hour reminder for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id,
                        appointment.Patient.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send 2-hour reminder for appointment {AppointmentId}",
                        appointment.Id);
                }
            }

            _logger.LogInformation("Completed 2-hour reminder job. Sent {Count} reminders", sentCount);
        }

        public async Task SendDailySummaryAsync()
        {
            _logger.LogInformation("Starting daily summary job");

            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var tomorrowEnd = tomorrow.AddDays(1);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Include(a => a.Slot)
                .Include(a => a.Clinician)
                    .ThenInclude(c => c.User)
                .Where(a => a.Status == AppointmentStatus.Confirmed
                    && a.Slot.StartDateTime >= tomorrow
                    && a.Slot.StartDateTime < tomorrowEnd)
                .ToListAsync();

            var groupedByPatient = appointments
                .GroupBy(a => a.Patient.UserId)
                .ToList();

            foreach (var group in groupedByPatient)
            {
                var patient = group.First().Patient;
                var appointmentList = group.ToList();

                try
                {
                    var message = $"You have {appointmentList.Count} appointment(s) tomorrow:\n" +
                        string.Join("\n", appointmentList.Select(a =>
                            $"- {a.Slot.StartDateTime:HH:mm} with {a.Clinician.User.FirstName} {a.Clinician.User.LastName}"));

                    await _notificationService.SendNotificationAsync(
                        patient.UserId,
                        "SystemNotification",
                        "Tomorrow's Appointments",
                        message);

                    _logger.LogInformation(
                        "Sent daily summary to patient {PatientId} with {Count} appointments",
                        patient.UserId,
                        appointmentList.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send daily summary to patient {PatientId}",
                        patient.UserId);
                }
            }

            _logger.LogInformation("Completed daily summary job. Processed {Count} patients", groupedByPatient.Count);
        }
    }
}