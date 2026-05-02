using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Services.Interfaces
{
    public interface IPatientService
    {
        Task<PatientProfileDto> GetPatientByIdAsync(string userId);
        Task<PatientProfileDto> UpdatePatientAsync(string userId, UpdatePatientRequest request);
        Task<IEnumerable<PatientNotificationDto>> GetPatientNotificationsAsync(string userId);
        Task MarkNotificationAsReadAsync(string userId, Guid notificationId);
    }
}