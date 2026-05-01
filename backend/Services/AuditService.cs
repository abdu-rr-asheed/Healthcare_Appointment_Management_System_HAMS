using HAMS.API.Data;
using HAMS.API.Models.Entities;
using HAMS.API.Services.Interfaces;

namespace HAMS.API.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string userName, string userRole, string action, string resourceType, Guid? resourceId, string ipAddress, string userAgent, string outcome, Dictionary<string, object>? details = null)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId),
                UserName = userName,
                UserRole = userRole,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Details = details,
                Outcome = outcome,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}