using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.DTOs.Requests
{
    public class CancelAppointmentRequest
    {
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public bool AcknowledgeLateNotice { get; set; }
    }
}