using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Models.DTOs.Requests
{
    public class RescheduleRequest
    {
        [Required]
        public Guid AppointmentId { get; set; }

        [Required]
        public Guid NewSlotId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}