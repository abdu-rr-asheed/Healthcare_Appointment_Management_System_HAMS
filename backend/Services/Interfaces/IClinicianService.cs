using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Services.Interfaces
{
    public interface IClinicianService
    {
        Task<ClinicianProfileDto> GetClinicianByIdAsync(string userId);
        Task<ClinicianAvailabilityDto> GetAvailabilityAsync(string userId);
        Task<ClinicianProfileDto> UpdateClinicianAsync(string userId, UpdateClinicianProfileRequestDto request);
        Task UpdateAvailabilityAsync(string userId, UpdateAvailabilityRequestDto request);
        Task<GenerateSlotsResponseDto> GenerateSlotsAsync(string userId, GenerateSlotsRequestDto request);
        Task<ScheduleResponseDto> GetScheduleAsync(string userId, string viewType, DateTime startDate);
        Task RemoveLeavePeriodAsync(Guid clinicianId, Guid leaveId, string callerUserId, bool isAdmin);
    }
}