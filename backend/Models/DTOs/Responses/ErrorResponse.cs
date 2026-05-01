namespace HAMS.API.Models.DTOs.Responses
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}