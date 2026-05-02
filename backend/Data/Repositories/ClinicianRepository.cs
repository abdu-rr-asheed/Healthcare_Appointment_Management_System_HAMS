using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public class ClinicianRepository : Repository<Clinician>, IClinicianRepository
{
    public ClinicianRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Clinician?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Clinicians
            .Include(c => c.User)
            .Include(c => c.Department)
            .Include(c => c.RegularSchedules)
            .Include(c => c.LeavePeriods)
            .Include(c => c.SlotConfigurations)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Clinician?> GetByClinicianIdAsync(Guid clinicianId)
    {
        return await _context.Clinicians
            .Include(c => c.User)
            .Include(c => c.Department)
            .Include(c => c.RegularSchedules)
            .Include(c => c.LeavePeriods)
            .Include(c => c.SlotConfigurations)
            .FirstOrDefaultAsync(c => c.ClinicianId == clinicianId);
    }

    public async Task<IEnumerable<Clinician>> GetByDepartmentAsync(Guid departmentId)
    {
        return await _context.Clinicians
            .Include(c => c.User)
            .Include(c => c.Department)
            .Where(c => c.DepartmentId == departmentId)
            .ToListAsync();
    }

    public async Task<ClinicianProfileDto> GetProfileAsync(Guid userId)
    {
        var clinician = await GetByUserIdAsync(userId);
        if (clinician == null) throw new KeyNotFoundException("Clinician not found");

        return new ClinicianProfileDto
        {
            Id = clinician.Id,
            UserId = userId,
            ClinicianId = clinician.ClinicianId,
            FirstName = clinician.User.FirstName,
            LastName = clinician.User.LastName,
            Email = clinician.User.Email,
            PhoneNumber = clinician.User.PhoneNumber,
            Specialty = clinician.Specialty,
            JobTitle = clinician.JobTitle,
            GmcNumber = clinician.GmcNumber,
            LicenseNumber = clinician.LicenseNumber,
            Qualifications = clinician.Qualifications,
            DepartmentId = clinician.DepartmentId,
            DepartmentName = clinician.Department.Name,
            Status = clinician.Status.ToString(),
            StartDate = clinician.StartDate,
            RegularSchedule = clinician.RegularSchedules.Select(rs => new RegularScheduleDto
            {
                DayOfWeek = rs.DayOfWeek,
                StartTime = rs.StartTime.ToString(@"hh\:mm"),
                EndTime = rs.EndTime.ToString(@"hh\:mm"),
                Recurring = rs.Recurring,
                IsAvailable = rs.IsAvailable
            }).ToList(),
            LeavePeriods = clinician.LeavePeriods.Select(lp => new LeavePeriodDto
            {
                LeavePeriodId = lp.Id,
                StartDate = lp.StartDate,
                EndDate = lp.EndDate,
                Reason = lp.Reason ?? string.Empty,
                Type = lp.Type,
                IsApproved = lp.IsApproved
            }).ToList(),
            SlotConfigurations = clinician.SlotConfigurations.Select(sc => new SlotConfigurationDto
            {
                AppointmentType = sc.AppointmentType,
                DurationMinutes = sc.DurationMinutes,
                BufferMinutes = sc.BufferMinutes
            }).ToList()
        };
    }

    public async Task<ClinicianAvailabilityDto> GetAvailabilityAsync(Guid userId)
    {
        var clinician = await GetByUserIdAsync(userId);
        if (clinician == null) throw new KeyNotFoundException("Clinician not found");

        return new ClinicianAvailabilityDto
        {
            ClinicianId = clinician.ClinicianId,
            RegularSchedule = clinician.RegularSchedules.Select(rs => new RegularScheduleDto
            {
                DayOfWeek = rs.DayOfWeek,
                StartTime = rs.StartTime.ToString(@"hh\:mm"),
                EndTime = rs.EndTime.ToString(@"hh\:mm"),
                IsAvailable = rs.IsAvailable
            }).ToList(),
            LeavePeriods = clinician.LeavePeriods.Where(lp => lp.EndDate >= DateTime.UtcNow)
                .Select(lp => new LeavePeriodDto
                {
                    LeavePeriodId = lp.Id,
                    StartDate = lp.StartDate,
                    EndDate = lp.EndDate,
                    Reason = lp.Reason ?? string.Empty,
                    Type = lp.Type,
                    IsApproved = lp.IsApproved
                }).ToList(),
            SlotConfigurations = clinician.SlotConfigurations.Select(sc => new SlotConfigurationDto
            {
                AppointmentType = sc.AppointmentType,
                DurationMinutes = sc.DurationMinutes,
                BufferMinutes = sc.BufferMinutes
            }).ToList()
        };
    }

    public async Task<IEnumerable<Clinician>> GetActiveCliniciansAsync()
    {
        return await _context.Clinicians
            .Include(c => c.User)
            .Include(c => c.Department)
            .Where(c => c.Status == ClinicianStatus.Active)
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinician>> GetCliniciansByStatusAsync(ClinicianStatus status)
    {
        return await _context.Clinicians
            .Include(c => c.User)
            .Include(c => c.Department)
            .Where(c => c.Status == status)
            .ToListAsync();
    }
}
