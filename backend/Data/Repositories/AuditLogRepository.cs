using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Data.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLogEntryDto>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<AuditLogEntryDto>> GetByActionAsync(AuditAction action, int page, int pageSize)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.Action == action.ToString())
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<AuditLogEntryDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<AuditLogResponse> QueryAsync(AuditLogQuery query)
    {
        var logsQuery = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(query.UserId) && Guid.TryParse(query.UserId, out var userIdGuid))
        {
            logsQuery = logsQuery.Where(a => a.UserId == userIdGuid);
        }

        if (!string.IsNullOrEmpty(query.ActionType))
        {
            logsQuery = logsQuery.Where(a => a.Action == query.ActionType);
        }

        if (query.StartDate.HasValue)
        {
            logsQuery = logsQuery.Where(a => a.Timestamp >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            logsQuery = logsQuery.Where(a => a.Timestamp <= query.EndDate.Value);
        }

        var totalCount = await logsQuery.CountAsync();

        var logs = await logsQuery
            .OrderByDescending(a => a.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new AuditLogResponse
        {
            Entries = logs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            CurrentPage = query.Page,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    public async Task<long> GetTotalCountAsync()
    {
        return await _context.AuditLogs.LongCountAsync();
    }

    private AuditLogEntryDto MapToDto(AuditLog log)
    {
        return new AuditLogEntryDto
        {
            Id = log.Id.ToString(),
            Timestamp = log.Timestamp,
            UserId = log.UserId,
            UserName = log.UserName,
            UserRole = log.UserRole ?? string.Empty,
            Action = log.Action,
            ResourceType = log.ResourceType,
            Resource = log.Resource ?? string.Empty,
            ResourceId = log.ResourceId?.ToString() ?? string.Empty,
            IpAddress = log.IpAddress ?? string.Empty,
            UserAgent = log.UserAgent ?? string.Empty,
            Details = log.Details,
            Outcome = log.Outcome
        };
    }
}
