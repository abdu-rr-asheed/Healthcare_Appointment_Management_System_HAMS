using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HAMS.API.Models.DTOs.Requests
{
    public class LoginRequest
    {
        [Required]
        [MaxLength(50)]
        public string NhsNumber { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}