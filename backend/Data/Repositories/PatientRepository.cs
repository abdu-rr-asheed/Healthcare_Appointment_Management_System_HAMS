using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Patient?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Clinician)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Patient?> GetByNhsNumberAsync(string nhsNumber)
    {
        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.Appointments)
            .FirstOrDefaultAsync(p => p.User.NhsNumber == nhsNumber);
    }

    public async Task<IEnumerable<Patient>> SearchByNameAsync(string firstName, string lastName)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(firstName))
        {
            query = query.Where(p => p.User.FirstName.ToLower().Contains(firstName.ToLower()));
        }

        if (!string.IsNullOrEmpty(lastName))
        {
            query = query.Where(p => p.User.LastName.ToLower().Contains(lastName.ToLower()));
        }

        return await query.ToListAsync();
    }

    public async Task<PatientProfileDto> GetProfileAsync(Guid userId)
    {
        var patient = await GetByUserIdAsync(userId);
        if (patient == null) throw new KeyNotFoundException("Patient not found");

        return new PatientProfileDto
        {
            Id = patient.Id,
            UserId = userId,
            NhsNumber = patient.User.NhsNumber,
            Email = patient.User.Email,
            PhoneNumber = patient.User.PhoneNumber,
            FirstName = patient.User.FirstName,
            LastName = patient.User.LastName,
            DateOfBirth = patient.User.DateOfBirth,
            Address = new AddressDto
            {
                Line1 = patient.Address,
                City = patient.City,
                Postcode = patient.Postcode
            },
            City = patient.City,
            Postcode = patient.Postcode,
            SmsOptIn = patient.SmsOptIn,
            EmergencyContactName = patient.EmergencyContactName,
            EmergencyContactPhone = patient.EmergencyContactPhone,
            CreatedAt = patient.User.CreatedAt,
            IsActive = patient.User.IsActive
        };
    }

    public async Task<IEnumerable<Patient>> GetPatientsByClinicianAsync(Guid clinicianId)
    {
        var appointmentPatientIds = await _context.Appointments
            .Where(a => a.ClinicianId == clinicianId)
            .Select(a => a.PatientId)
            .Distinct()
            .ToListAsync();

        return await _context.Patients
            .Include(p => p.User)
            .Where(p => appointmentPatientIds.Contains(p.Id))
            .ToListAsync();
    }
}
