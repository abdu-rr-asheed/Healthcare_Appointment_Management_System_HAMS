using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public interface IClinicianRepository : IRepository<Clinician>
{
    Task<Clinician?> GetByUserIdAsync(Guid userId);
    Task<Clinician?> GetByClinicianIdAsync(Guid clinicianId);
    Task<IEnumerable<Clinician>> GetByDepartmentAsync(Guid departmentId);
    Task<ClinicianProfileDto> GetProfileAsync(Guid userId);
    Task<ClinicianAvailabilityDto> GetAvailabilityAsync(Guid userId);
    Task<IEnumerable<Clinician>> GetActiveCliniciansAsync();
    Task<IEnumerable<Clinician>> GetCliniciansByStatusAsync(ClinicianStatus status);
}
