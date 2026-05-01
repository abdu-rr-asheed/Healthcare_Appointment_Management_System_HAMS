using HAMS.API.Models.DTOs.Requests;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid departmentId, Guid? clinicianId, DateTime startDate, DateTime endDate);
        Task<AppointmentDto> BookAppointmentAsync(string userId, BookAppointmentRequest request);
        Task<AppointmentDto> RescheduleAppointmentAsync(string userId, Guid appointmentId, RescheduleRequest request);
        Task<bool> CancelAppointmentAsync(string userId, Guid appointmentId, CancelAppointmentRequest request);
        Task<IEnumerable<AppointmentDto>> GetUpcomingAppointmentsAsync(string userId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentHistoryAsync(string userId, string? status, DateTime? startDate, DateTime? endDate);
        Task<AppointmentDto?> GetAppointmentByIdAsync(Guid appointmentId, string userId);
        Task<bool> MarkAsDidNotAttendAsync(Guid appointmentId, string reason);
    }
}