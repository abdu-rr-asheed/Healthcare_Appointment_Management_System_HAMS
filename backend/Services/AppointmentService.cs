using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;

namespace HAMS.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AppointmentService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid departmentId, Guid? clinicianId, DateTime startDate, DateTime endDate)
        {
            var query = _context.AvailabilitySlots
                .Include(s => s.Clinician)
                .Include(s => s.Clinician.User)
                .Include(s => s.Department)
                .Where(s => s.DepartmentId == departmentId && s.IsAvailable && !s.IsCancelled)
                .Where(s => s.StartDateTime >= startDate && s.EndDateTime <= endDate);

            if (clinicianId.HasValue)
            {
                query = query.Where(s => s.ClinicianId == clinicianId.Value);
            }

            var slots = await query.ToListAsync();

            return slots.Select(s => new AvailableSlotDto
            {
                Id = s.Id,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                ClinicianId = s.ClinicianId,
                ClinicianName = $"{s.Clinician.User.FirstName} {s.Clinician.User.LastName}",
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department.Name,
                IsAvailable = s.IsAvailable
            });
        }

        public async Task<AppointmentDto> BookAppointmentAsync(string userId, BookAppointmentRequest request)
        {
            var user = await _context.Users.Include(u => u.Patient).FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null || user.Patient == null)
            {
                throw new UnauthorizedAccessException("Patient not found");
            }

            var slot = await _context.AvailabilitySlots
                .Include(s => s.Clinician)
                .Include(s => s.Clinician.User)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == request.SlotId && s.IsAvailable);

            if (slot == null)
            {
                throw new Exception("Slot not available");
            }

            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.SlotId == request.SlotId && a.Status != AppointmentStatus.Cancelled);

            if (existingAppointment)
            {
                throw new Exception("Slot already booked");
            }

            var confirmationReference = GenerateConfirmationReference();

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                ConfirmationReference = confirmationReference,
                PatientId = user.Patient.Id,
                SlotId = slot.Id,
                Type = Enum.Parse<AppointmentType>(request.AppointmentType),
                Notes = request.Notes,
                Status = AppointmentStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);

            slot.IsAvailable = false;
            _context.AvailabilitySlots.Update(slot);

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString(),
                "AppointmentBooked",
                "Appointment",
                appointment.Id,
                "",
                "",
                "Success"
            );

            return MapToAppointmentDto(appointment, slot);
        }

        public async Task<AppointmentDto> RescheduleAppointmentAsync(string userId, Guid appointmentId, RescheduleRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .ThenInclude(s => s.Clinician)
                .Include(a => a.Slot)
                .ThenInclude(s => s.Department)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new Exception("Appointment not found");
            }

            if (appointment.Patient.UserId.ToString() != userId && user.Role != UserRole.Administrator)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            var newSlot = await _context.AvailabilitySlots
                .Include(s => s.Clinician)
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == request.NewSlotId && s.IsAvailable);

            if (newSlot == null)
            {
                throw new Exception("New slot not available");
            }

            var oldSlot = appointment.Slot;
            oldSlot.IsAvailable = true;

            newSlot.IsAvailable = false;

            appointment.SlotId = newSlot.Id;
            appointment.RescheduleReason = request.Reason;
            appointment.RescheduledAt = DateTime.UtcNow;
            appointment.ConfirmationReference = GenerateConfirmationReference();

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString(),
                "AppointmentRescheduled",
                "Appointment",
                appointment.Id,
                "",
                "",
                "Success"
            );

            return MapToAppointmentDto(appointment, newSlot);
        }

        public async Task<bool> CancelAppointmentAsync(string userId, Guid appointmentId, CancelAppointmentRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.Patient)
                .Include(a => a.Patient.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new Exception("Appointment not found");
            }

            if (appointment.Patient.UserId.ToString() != userId && user.Role != UserRole.Administrator)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Reason;
            appointment.CancelledAt = DateTime.UtcNow;

            appointment.Slot.IsAvailable = true;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                $"{user.FirstName} {user.LastName}",
                user.Role.ToString(),
                "AppointmentCancelled",
                "Appointment",
                appointment.Id,
                "",
                "",
                "Success"
            );

            return true;
        }

        public async Task<IEnumerable<AppointmentDto>> GetUpcomingAppointmentsAsync(string userId)
        {
            var user = await _context.Users.Include(u => u.Patient).Include(u => u.Clinician).FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            IQueryable<Appointment> query;

            if (user.Role == UserRole.Patient && user.Patient != null)
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Where(a => a.PatientId == user.Patient.Id);
            }
            else if (user.Role == UserRole.Clinician && user.Clinician != null)
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Where(a => a.Slot.ClinicianId == user.Clinician.Id);
            }
            else
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User);
            }

            var appointments = await query
                .Where(a => a.Status == AppointmentStatus.Confirmed)
                .Where(a => a.Slot.StartDateTime >= DateTime.UtcNow)
                .OrderBy(a => a.Slot.StartDateTime)
                .ToListAsync();

            return appointments.Select(a => MapToAppointmentDto(a, a.Slot));
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentHistoryAsync(string userId, string? status, DateTime? startDate, DateTime? endDate)
        {
            var user = await _context.Users.Include(u => u.Patient).Include(u => u.Clinician).FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            IQueryable<Appointment> query;

            if (user.Role == UserRole.Patient && user.Patient != null)
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Where(a => a.PatientId == user.Patient.Id);
            }
            else if (user.Role == UserRole.Clinician && user.Clinician != null)
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Where(a => a.Slot.ClinicianId == user.Clinician.Id);
            }
            else
            {
                query = _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Slot.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Slot.Department)
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status.ToString() == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Slot.StartDateTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Slot.StartDateTime <= endDate.Value);
            }

            var appointments = await query.OrderByDescending(a => a.Slot.StartDateTime).ToListAsync();

            return appointments.Select(a => MapToAppointmentDto(a, a.Slot));
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(Guid appointmentId, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.Slot.Clinician)
                .Include(a => a.Slot.Clinician.User)
                .Include(a => a.Slot.Department)
                .Include(a => a.Patient)
                .Include(a => a.Patient.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return null;
            }

            if (user.Role == UserRole.Patient && appointment.Patient.UserId.ToString() != userId)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            if (user.Role == UserRole.Clinician && appointment.Slot.Clinician.UserId.ToString() != userId)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            return MapToAppointmentDto(appointment, appointment.Slot);
        }

        public async Task<bool> MarkAsDidNotAttendAsync(Guid appointmentId, string reason)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new Exception("Appointment not found");
            }

            appointment.Status = AppointmentStatus.DidNotAttend;
            appointment.DidNotAttendAt = DateTime.UtcNow;
            appointment.DnaReason = reason;

            await _context.SaveChangesAsync();

            return true;
        }

        private AppointmentDto MapToAppointmentDto(Appointment appointment, AvailabilitySlot slot)
        {
            return new AppointmentDto
            {
                Id = appointment.Id,
                ConfirmationReference = appointment.ConfirmationReference,
                StartDateTime = slot.StartDateTime,
                EndDateTime = slot.EndDateTime,
                Status = appointment.Status.ToString(),
                AppointmentType = appointment.Type.ToString(),
                Notes = appointment.Notes,
                Patient = new PatientDto
                {
                    Id = appointment.Patient.Id,
                    UserId = appointment.Patient.UserId,
                    NhsNumber = appointment.Patient.User.NhsNumber,
                    FirstName = appointment.Patient.User.FirstName,
                    LastName = appointment.Patient.User.LastName,
                    Email = appointment.Patient.User.Email,
                    PhoneNumber = appointment.Patient.User.PhoneNumber,
                    DateOfBirth = appointment.Patient.User.DateOfBirth
                },
                Clinician = new ClinicianDto
                {
                    Id = slot.Clinician.Id,
                    UserId = slot.Clinician.UserId,
                    FirstName = slot.Clinician.User.FirstName,
                    LastName = slot.Clinician.User.LastName,
                    Specialty = slot.Clinician.Specialty,
                    LicenseNumber = slot.Clinician.LicenseNumber,
                    Qualifications = slot.Clinician.Qualifications,
                    Status = slot.Clinician.Status.ToString()
                },
                Department = new DepartmentDto
                {
                    Id = slot.Department.Id,
                    Name = slot.Department.Name,
                    Description = slot.Department.Description
                },
                CreatedAt = appointment.CreatedAt
            };
        }

        private string GenerateConfirmationReference()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var result = new char[10];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }
    }
}