using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLogEntryDto>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<IEnumerable<AuditLogEntryDto>> GetByActionAsync(AuditAction action, int page, int pageSize);
    Task<IEnumerable<AuditLogEntryDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize);
    Task<AuditLogResponse> QueryAsync(AuditLogQuery query);
    Task<long> GetTotalCountAsync();
}
