using HAMS.API.Data;
using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HAMS.API.Services
{
    public class ClinicianService : IClinicianService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClinicianService> _logger;

        public ClinicianService(
            ApplicationDbContext context,
            ILogger<ClinicianService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClinicianProfileDto> GetClinicianByIdAsync(string userId)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.User)
                .Include(c => c.Department)
                .Include(c => c.RegularSchedules)
                .Include(c => c.LeavePeriods)
                .Include(c => c.SlotConfigurations)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            return new ClinicianProfileDto
            {
                Id = clinician.Id,
                UserId = clinician.UserId,
                ClinicianId = clinician.ClinicianId,
                FirstName = clinician.User.FirstName,
                LastName = clinician.User.LastName,
                Email = clinician.User.Email,
                PhoneNumber = clinician.User.PhoneNumber ?? string.Empty,
                Specialty = clinician.Specialty,
                DepartmentId = clinician.DepartmentId,
                DepartmentName = clinician.Department.Name,
                LicenseNumber = clinician.LicenseNumber,
                Qualifications = clinician.Qualifications,
                Status = clinician.Status.ToString(),
                JobTitle = clinician.JobTitle,
                GmcNumber = clinician.GmcNumber,
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

        public async Task<ClinicianProfileDto> UpdateClinicianAsync(string userId, UpdateClinicianProfileRequestDto request)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            if (request.FirstName != null)
                clinician.User.FirstName = request.FirstName;
            if (request.LastName != null)
                clinician.User.LastName = request.LastName;
            if (request.Email != null)
                clinician.User.Email = request.Email;
            if (request.PhoneNumber != null)
                clinician.User.PhoneNumber = request.PhoneNumber;
            if (request.Specialty != null)
                clinician.Specialty = request.Specialty;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Clinician profile updated for userId: {UserId}", userId);

            return await GetClinicianByIdAsync(userId);
        }

        public async Task<ClinicianAvailabilityDto> GetAvailabilityAsync(string userId)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.RegularSchedules)
                .Include(c => c.LeavePeriods)
                .Include(c => c.SlotConfigurations)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            return new ClinicianAvailabilityDto
            {
                ClinicianId = clinician.ClinicianId,
                RegularSchedule = clinician.RegularSchedules.Select(rs => new RegularScheduleDto
                {
                    DayOfWeek = rs.DayOfWeek,
                    StartTime = rs.StartTime.ToString(@"hh\:mm"),
                    EndTime = rs.EndTime.ToString(@"hh\:mm"),
                    IsAvailable = rs.IsAvailable,
                    Recurring = rs.Recurring
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

        public async Task UpdateAvailabilityAsync(string userId, UpdateAvailabilityRequestDto request)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.RegularSchedules)
                .Include(c => c.LeavePeriods)
                .Include(c => c.SlotConfigurations)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            var existingSchedules = await _context.RegularSchedules.Where(rs => rs.ClinicianId == clinician.Id).ToListAsync();
            _context.RegularSchedules.RemoveRange(existingSchedules);

            if (request.RegularSchedule != null)
            {
                var newSchedules = request.RegularSchedule.Select(rs => new RegularSchedule
                {
                    Id = Guid.NewGuid(),
                    ClinicianId = clinician.Id,
                    DayOfWeek = rs.DayOfWeek,
                    StartTime = TimeSpan.Parse(rs.StartTime),
                    EndTime = TimeSpan.Parse(rs.EndTime),
                    IsAvailable = rs.IsAvailable,
                    Recurring = rs.Recurring,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
                await _context.RegularSchedules.AddRangeAsync(newSchedules);
            }

            if (request.LeavePeriods != null)
            {
                foreach (var leave in request.LeavePeriods)
                {
                    if (leave.LeavePeriodId == Guid.Empty)
                    {
                        clinician.LeavePeriods.Add(new LeavePeriod
                        {
                            ClinicianId = clinician.Id,
                            StartDate = leave.StartDate,
                            EndDate = leave.EndDate,
                            Reason = leave.Reason,
                            IsApproved = leave.IsApproved
                        });
                    }
                }
            }

            if (request.SlotConfigurations != null)
            {
                _context.SlotConfigurations.RemoveRange(clinician.SlotConfigurations);
                clinician.SlotConfigurations = request.SlotConfigurations.Select(sc => new SlotConfiguration
                {
                    ClinicianId = clinician.ClinicianId,
                    AppointmentType = sc.AppointmentType,
                    DurationMinutes = sc.DurationMinutes,
                    BufferMinutes = sc.BufferMinutes
                }).ToList();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Availability updated for clinician userId: {UserId}", userId);
        }

        public async Task<HAMS.API.Models.DTOs.Responses.GenerateSlotsResponseDto> GenerateSlotsAsync(string userId, HAMS.API.Models.DTOs.Requests.GenerateSlotsRequestDto request)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.RegularSchedules)
                .Include(c => c.LeavePeriods)
                .Include(c => c.SlotConfigurations)
                .Include(c => c.AppointmentSlots)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            var response = new GenerateSlotsResponseDto();
            var warnings = new List<string>();

            var existingSlots = await _context.AppointmentSlots
                .Where(s => s.ClinicianId == clinician.ClinicianId
                    && s.StartDateTime >= request.StartDate
                    && s.EndDateTime <= request.EndDate)
                .ToListAsync();

            var dates = Enumerable.Range(0, (int)(request.EndDate - request.StartDate).TotalDays + 1)
                .Select(d => request.StartDate.AddDays(d))
                .ToList();

            foreach (var date in dates)
            {
                var dayOfWeek = (int)date.DayOfWeek;
                var regularSchedule = clinician.RegularSchedules
                    .FirstOrDefault(rs => rs.DayOfWeek == dayOfWeek && rs.IsAvailable);

                if (regularSchedule == null)
                {
                    warnings.Add($"No schedule defined for {dayOfWeek}");
                    continue;
                }

                var isOnLeave = clinician.LeavePeriods.Any(lp =>
                    date.Date >= lp.StartDate.Date && date.Date <= lp.EndDate.Date);

                if (isOnLeave)
                {
                    response.SlotsBlocked++;
                    continue;
                }

                var defaultConfig = clinician.SlotConfigurations
                    .FirstOrDefault(sc => sc.AppointmentType == "Standard")
                    ?? new SlotConfiguration { DurationMinutes = 30, BufferMinutes = 5 };

                var currentTime = date.Date.Add(regularSchedule.StartTime);
                var endTime = date.Date.Add(regularSchedule.EndTime);

                while (currentTime.Add(TimeSpan.FromMinutes(defaultConfig.DurationMinutes)) <= endTime)
                {
                    var slotStart = currentTime;
                    var slotEnd = slotStart.AddMinutes(defaultConfig.DurationMinutes);

                    if (slotStart > DateTime.Now)
                    {
                        var existingSlot = existingSlots.FirstOrDefault(s =>
                            s.StartDateTime == slotStart && s.EndDateTime == slotEnd);

                        if (existingSlot == null)
                        {
                            var newSlot = new AppointmentSlot
                            {
                                ClinicianId = clinician.ClinicianId,
                                StartDateTime = slotStart,
                                EndDateTime = slotEnd,
                                Status = SlotStatus.Available,
                                Type = "Standard"
                            };
                            _context.AppointmentSlots.Add(newSlot);
                            response.SlotsGenerated++;
                        }
                    }

                    currentTime = currentTime.AddMinutes(defaultConfig.DurationMinutes + defaultConfig.BufferMinutes);
                }
            }

            response.Warnings = warnings;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Generated {Generated} slots for clinician {ClinicianId}",
                response.SlotsGenerated,
                clinician.ClinicianId);

            return response;
        }

        public async Task RemoveLeavePeriodAsync(Guid clinicianId, Guid leaveId, string callerUserId, bool isAdmin)
        {
            // Resolve the clinician record and verify it belongs to the caller
            // (unless the caller is an Administrator).
            var clinician = await _context.Clinicians
                .FirstOrDefaultAsync(c => c.Id == clinicianId)
                ?? throw new KeyNotFoundException("Clinician not found");

            if (!isAdmin && clinician.UserId.ToString() != callerUserId)
                throw new UnauthorizedAccessException("You may only remove your own leave periods");

            var leave = await _context.LeavePeriods
                .FirstOrDefaultAsync(lp => lp.Id == leaveId && lp.ClinicianId == clinicianId)
                ?? throw new KeyNotFoundException("Leave period not found");

            _context.LeavePeriods.Remove(leave);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Leave period {LeaveId} removed for clinician {ClinicianId} by {CallerUserId}",
                leaveId, clinicianId, callerUserId);
        }

        public async Task<ScheduleResponseDto> GetScheduleAsync(string userId, string viewType, DateTime startDate)
        {
            var userIdGuid = Guid.Parse(userId);
            var clinician = await _context.Clinicians
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid);

            if (clinician == null)
                throw new KeyNotFoundException("Clinician not found");

            var endDate = viewType.ToLower() switch
            {
                "day" => startDate.AddDays(1),
                "week" => startDate.AddDays(7),
                "month" => startDate.AddMonths(1),
                _ => startDate.AddDays(7)
            };

            var slots = await _context.AvailabilitySlots
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Where(s => s.ClinicianId == clinician.Id
                    && s.StartDateTime >= startDate
                    && s.StartDateTime < endDate)
                .OrderBy(s => s.StartDateTime)
                .ToListAsync();

            var appointments = slots
                .SelectMany(s => s.Appointments
                    .Where(a => a.Status != AppointmentStatus.Cancelled)
                    .Select(a => new ScheduledAppointmentDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = a.Patient?.User != null
                            ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}"
                            : "Unknown",
                        PatientNhsNumber = a.Patient?.User?.NhsNumber ?? string.Empty,
                        StartDateTime = s.StartDateTime,
                        EndDateTime = s.EndDateTime,
                        AppointmentType = a.Type.ToString(),
                        Status = a.Status.ToString(),
                        Department = clinician.Department?.Name ?? string.Empty,
                        EhrFlags = new List<EhrFlagDto>(),
                        HasClinicalNotes = false,
                        IsFollowUpRequired = false,
                        AppointmentSlotId = s.Id,
                        SlotId = s.Id
                    }))
                .ToList();

            return new ScheduleResponseDto
            {
                ClinicianId = clinician.Id.ToString(),
                ViewType = viewType,
                DateRange = new DateRangeDto
                {
                    Start = startDate,
                    End = endDate
                },
                Appointments = appointments
            };
        }
    }
}