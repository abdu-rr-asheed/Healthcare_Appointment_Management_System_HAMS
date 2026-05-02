using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetByClinicianIdAsync(Guid clinicianId);
    Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Appointment>> GetUpcomingAppointmentsAsync(Guid userId, int limit = 10);
    Task<IEnumerable<Appointment>> GetByStatusAsync(AppointmentStatus status);
    Task<Appointment?> GetByConfirmationReferenceAsync(string reference);
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid departmentId, Guid? clinicianId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<AppointmentHistoryItemDto>> GetHistoryAsync(Guid userId, DateTime? startDate, DateTime? endDate, AppointmentStatus? status, int page, int pageSize);
    Task<int> GetTotalAppointmentsCountAsync();
    Task<int> GetAppointmentsByStatusCountAsync(AppointmentStatus status);
}
