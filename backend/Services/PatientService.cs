using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HAMS.API.Services
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEhrIntegrationService _ehrService;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            ApplicationDbContext context,
            IEhrIntegrationService ehrService,
            ILogger<PatientService> logger)
        {
            _context = context;
            _ehrService = ehrService;
            _logger = logger;
        }

        public async Task<PatientProfileDto> GetPatientByIdAsync(string userId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Id == Guid.Parse(userId));

            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var ehrPatient = await _ehrService.GetPatientByNhsNumberAsync(patient.NhsNumber);
            var allergies = await _ehrService.GetAllergiesAsync(patient.NhsNumber);
            var medications = await _ehrService.GetMedicationsAsync(patient.NhsNumber);
            var medicalHistory = await _ehrService.GetMedicalHistoryAsync(patient.NhsNumber);

            return new PatientProfileDto
            {
                Id = patient.Id,
                UserId = patient.UserId,
                NhsNumber = patient.NhsNumber,
                FirstName = patient.User.FirstName,
                LastName = patient.User.LastName,
                Email = patient.User.Email,
                PhoneNumber = patient.User.PhoneNumber,
                DateOfBirth = patient.User.DateOfBirth,
                Address = new AddressDto
                {
                    Line1 = patient.Address,
                    City = patient.City,
                    Postcode = patient.Postcode
                },
                City = patient.City,
                Postcode = patient.Postcode,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                GpPractice = string.Empty,
                Allergies = allergies?.Allergies?.Select(a => a.Substance).ToList() != null 
                    ? string.Join(", ", allergies.Allergies.Select(a => a.Substance)) 
                    : string.Empty,
                MedicalConditions = medicalHistory?.ChronicConditions?.Select(c => c.Name).ToList() != null 
                    ? string.Join(", ", medicalHistory.ChronicConditions.Select(c => c.Name)) 
                    : string.Empty,
                CurrentMedications = medications?.Medications?.Select(m => m.Name).ToList() != null 
                    ? string.Join(", ", medications.Medications.Select(m => m.Name)) 
                    : string.Empty,
                ProfileImageUrl = patient.ProfileImageUrl,
                SmsOptIn = patient.SmsOptIn
            };
        }

        public async Task<PatientProfileDto> UpdatePatientAsync(string userId, UpdatePatientRequest request)
        {
            var guidUserId = Guid.Parse(userId);
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == guidUserId);

            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            if (request.PhoneNumber != null)
                patient.User.PhoneNumber = request.PhoneNumber;
            if (request.Address != null)
                patient.Address = request.Address;
            if (request.Postcode != null)
                patient.Postcode = request.Postcode;
            if (request.EmergencyContactName != null)
                patient.EmergencyContactName = request.EmergencyContactName;
            if (request.EmergencyContactPhone != null)
                patient.EmergencyContactPhone = request.EmergencyContactPhone;

            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Patient profile updated for userId: {UserId}", userId);

            return await GetPatientByIdAsync(userId);
        }

        public async Task<IEnumerable<PatientNotificationDto>> GetPatientNotificationsAsync(string userId)
        {
            var guidUserId = Guid.Parse(userId);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == guidUserId);
            if (patient == null)
                return Enumerable.Empty<PatientNotificationDto>();

            var notifications = await _context.Notifications
                .Where(n => n.PatientId == patient.Id && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PatientNotificationDto
                {
                    NotificationId = n.Id,
                    Type = n.Type,
                    Title = n.Subject,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    AppointmentId = n.AppointmentId ?? Guid.Empty,
                    IsRead = n.IsRead
                })
                .ToListAsync();

            return notifications;
        }

        public async Task MarkNotificationAsReadAsync(string userId, Guid notificationId)
        {
            var guidUserId = Guid.Parse(userId);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == guidUserId);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.PatientId == patient.Id);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found");

            notification.IsRead = true;

            await _context.SaveChangesAsync();
        }
    }
}