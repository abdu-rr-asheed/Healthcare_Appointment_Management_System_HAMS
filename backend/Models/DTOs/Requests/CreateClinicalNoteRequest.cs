using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.DTOs.Requests
{
    public class CreateClinicalNoteRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = false;

        [MaxLength(100)]
        public string ConsultationType { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Findings { get; set; }

        [MaxLength(5000)]
        public string? Recommendations { get; set; }
    }
}