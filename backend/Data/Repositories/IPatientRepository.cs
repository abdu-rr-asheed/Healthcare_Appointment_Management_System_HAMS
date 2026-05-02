using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByUserIdAsync(Guid userId);
    Task<Patient?> GetByNhsNumberAsync(string nhsNumber);
    Task<IEnumerable<Patient>> SearchByNameAsync(string firstName, string lastName);
    Task<PatientProfileDto> GetProfileAsync(Guid userId);
    Task<IEnumerable<Patient>> GetPatientsByClinicianAsync(Guid clinicianId);
}
