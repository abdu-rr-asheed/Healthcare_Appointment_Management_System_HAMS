using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Clinician)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Slot.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByClinicianIdAsync(Guid clinicianId)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Patient.User)
            .Include(a => a.Clinician)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .Where(a => a.ClinicianId == clinicianId)
            .OrderByDescending(a => a.Slot.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Patient.User)
            .Include(a => a.Clinician)
            .Include(a => a.Clinician.User)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .Where(a => a.Slot.StartDateTime >= startDate && a.Slot.StartDateTime <= endDate)
            .OrderBy(a => a.Slot.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid userId, int limit = 10)
    {
        var now = DateTime.UtcNow;
        
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Patient.User)
            .Include(a => a.Clinician)
            .Include(a => a.Clinician.User)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .Where(a => a.PatientId == userId || a.ClinicianId == userId)
            .Where(a => a.Slot.StartDateTime >= now && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Slot.StartDateTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByStatusAsync(AppointmentStatus status)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Clinician)
            .Include(a => a.Slot)
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.Slot.StartDateTime)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByConfirmationReferenceAsync(string reference)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Clinician)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.ConfirmationReference == reference);
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid departmentId, Guid? clinicianId, DateTime startDate, DateTime endDate)
    {
        var query = _context.AvailabilitySlots
            .Where(s => s.DepartmentId == departmentId)
            .Where(s => s.StartDateTime >= startDate && s.StartDateTime <= endDate)
            .Where(s => s.IsAvailable && !s.IsCancelled);

        if (clinicianId.HasValue)
        {
            query = query.Where(s => s.ClinicianId == clinicianId.Value);
        }

        var slots = await query
            .Include(s => s.Clinician)
            .Include(s => s.Clinician.User)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync();

        return slots.Select(s => new AvailableSlotDto
        {
            Id = s.Id,
            StartDateTime = s.StartDateTime,
            EndDateTime = s.EndDateTime,
            ClinicianId = s.ClinicianId,
            ClinicianName = $"{s.Clinician.User.FirstName} {s.Clinician.User.LastName}",
            DepartmentId = s.DepartmentId,
            IsAvailable = s.IsAvailable
        }).ToList();
    }

    public async Task<IEnumerable<AppointmentHistoryItemDto>> GetHistoryAsync(
        Guid userId, 
        DateTime? startDate, 
        DateTime? endDate, 
        AppointmentStatus? status, 
        int page, 
        int pageSize)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Clinician)
            .Include(a => a.Clinician.User)
            .Include(a => a.Department)
            .Include(a => a.Slot)
            .Where(a => a.PatientId == userId || a.ClinicianId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Slot.StartDateTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Slot.StartDateTime <= endDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var appointments = await query
            .OrderByDescending(a => a.Slot.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return appointments.Select(a => new AppointmentHistoryItemDto
        {
            Id = a.Id.ToString(),
            ConfirmationReference = a.ConfirmationReference,
            Date = a.Slot.StartDateTime.ToString("yyyy-MM-dd"),
            Time = a.Slot.StartDateTime.ToString("HH:mm"),
            ClinicianName = $"{a.Clinician.User.FirstName} {a.Clinician.User.LastName}",
            DepartmentName = a.Department.Name,
            Status = a.Status.ToString(),
            Notes = a.Notes
        }).ToList();
    }

    public async Task<int> GetTotalAppointmentsCountAsync()
    {
        return await _context.Appointments.CountAsync();
    }

    public async Task<int> GetAppointmentsByStatusCountAsync(AppointmentStatus status)
    {
        return await _context.Appointments.CountAsync(a => a.Status == status);
    }
}
