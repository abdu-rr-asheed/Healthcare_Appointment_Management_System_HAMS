using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using System.Text;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    [Produces("application/json")]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditController> _logger;

        public AuditController(ApplicationDbContext context, ILogger<AuditController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponseDto<AuditLogEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLog(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    var userIdGuid = Guid.Parse(userId);
                    query = query.Where(a => a.UserId == userIdGuid);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action == action);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var entries = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new PaginatedResponseDto<AuditLogEntryDto>
                {
                    Items = entries.Select(e => new AuditLogEntryDto
                    {
                        Id = e.Id.ToString(),
                        Timestamp = e.Timestamp,
                        UserId = e.UserId,
                        UserName = e.User?.UserName ?? string.Empty,
                        UserRole = e.UserRole ?? string.Empty,
                        Action = e.Action,
                        ResourceType = e.ResourceType ?? string.Empty,
                        ResourceId = e.ResourceId?.ToString() ?? string.Empty,
                        IpAddress = e.IpAddress ?? string.Empty,
                        Outcome = e.Outcome
                    }).ToList(),
                    TotalCount = totalCount,
                    CurrentPage = page,
                    TotalPages = totalPages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit log");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AuditLogEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuditLogEntry(Guid id)
        {
            try
            {
                var entry = await _context.AuditLogs
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (entry == null)
                {
                    return NotFound(new ErrorResponse { Message = "Audit log entry not found" });
                }

                var result = new AuditLogEntryDto
                {
                    Id = entry.Id.ToString(),
                    Timestamp = entry.Timestamp,
                    UserId = entry.UserId,
                    UserName = entry.User?.UserName ?? string.Empty,
                    UserRole = entry.UserRole ?? string.Empty,
                    Action = entry.Action,
                    ResourceType = entry.ResourceType ?? string.Empty,
                    ResourceId = entry.ResourceId?.ToString() ?? string.Empty,
                    IpAddress = entry.IpAddress ?? string.Empty,
                    Outcome = entry.Outcome
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit log entry");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }

        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportAuditLog(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string format = "CSV")
        {
            try
            {
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    var userIdGuid = Guid.Parse(userId);
                    query = query.Where(a => a.UserId == userIdGuid);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action == action);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                var entries = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10000)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,UserId,UserName,Action,ResourceType,ResourceId,IpAddress,Outcome");

                foreach (var entry in entries)
                {
                    csv.AppendLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss},{entry.UserId},{entry.User?.UserName},{entry.Action},{entry.ResourceType},{entry.ResourceId},{entry.IpAddress},{entry.Outcome}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export audit log");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred" });
            }
        }
    }
}