using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HAMS.API.Models.DTOs.Requests
{
    public class MfaVerificationRequest
    {
        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;
    }
}