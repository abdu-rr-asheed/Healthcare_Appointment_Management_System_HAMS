using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;

namespace HAMS.API.Data.Repositories;

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Department?> GetByNameAsync(string name)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Department>> GetAllActiveAsync()
    {
        return await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinician>> GetCliniciansByDepartmentAsync(Guid departmentId)
    {
        var department = await _context.Departments
            .Include(d => d.Clinicians)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        return department?.Clinicians ?? new List<Clinician>();
    }

    public async Task<int> GetAppointmentCountByDepartmentAsync(Guid departmentId, DateTime startDate, DateTime endDate)
    {
        return await _context.Appointments
            .Include(a => a.Slot)
            .CountAsync(a => a.DepartmentId == departmentId 
                && a.Slot.StartDateTime >= startDate 
                && a.Slot.StartDateTime <= endDate);
    }
}
