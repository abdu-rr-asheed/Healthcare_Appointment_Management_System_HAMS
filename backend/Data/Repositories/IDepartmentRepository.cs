using HAMS.API.Models.Entities;

namespace HAMS.API.Data.Repositories;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<Department?> GetByNameAsync(string name);
    Task<IEnumerable<Department>> GetAllActiveAsync();
    Task<IEnumerable<Clinician>> GetCliniciansByDepartmentAsync(Guid departmentId);
    Task<int> GetAppointmentCountByDepartmentAsync(Guid departmentId, DateTime startDate, DateTime endDate);
}
