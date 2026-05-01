using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.DTOs.Requests
{
    public class BookAppointmentRequest
    {
        [Required]
        public Guid SlotId { get; set; }

        [Required]
        public string AppointmentType { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}