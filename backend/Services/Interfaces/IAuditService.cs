namespace HAMS.API.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string userName, string userRole, string action, string resourceType, Guid? resourceId, string ipAddress, string userAgent, string outcome, Dictionary<string, object>? details = null);
    }
}