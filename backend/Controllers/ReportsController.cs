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
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateReportResponseDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequestDto request)
        {
            try
            {
                var reportId = Guid.NewGuid();
                var reportData = await GenerateReportDataAsync(request);

                var csvContent = GenerateCsvContent(reportData);
                var downloadUrl = $"/api/reports/{reportId}/download?format=csv";

                var expiresAt = DateTime.UtcNow.AddHours(24);

                return Accepted(new GenerateReportResponseDto
                {
                    ReportId = reportId,
                    Status = "Completed",
                    DownloadUrl = downloadUrl,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ReportDataDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReport(Guid id)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Slot)
                    .Include(a => a.Department)
                    .ToListAsync();

                var summary = new ReportSummaryDto
                {
                    TotalBookings = appointments.Count,
                    TotalCancellations = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                    TotalDna = appointments.Count(a => a.Status == AppointmentStatus.DidNotAttend)
                };

                return Ok(new ReportDataDto
                {
                    Summary = summary,
                    Details = new List<ReportDetailDto>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get report");
                return NotFound(new ErrorResponse { Message = "Report not found" });
            }
        }

        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadReport(Guid id, [FromQuery] string format = "CSV")
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Patient.User)
                    .Include(a => a.Slot)
                    .Include(a => a.Slot!.Clinician)
                    .Include(a => a.Slot.Clinician.User)
                    .Include(a => a.Department)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("ConfirmationReference,PatientName,ClinicianName,Department,DateTime,Status,Type");

                foreach (var apt in appointments)
                {
                    csv.AppendLine($"{apt.ConfirmationReference},{apt.Patient.User.FirstName} {apt.Patient.User.LastName},{apt.Slot.Clinician.User.FirstName} {apt.Slot.Clinician.User.LastName},{apt.Department.Name},{apt.Slot.StartDateTime:yyyy-MM-dd HH:mm},{apt.Status},{apt.Type}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"report_{id}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download report");
                return NotFound(new ErrorResponse { Message = "Report not found" });
            }
        }

        [HttpGet("types")]
        [ProducesResponseType(typeof(IEnumerable<ReportTypeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportTypes()
        {
            var types = new List<ReportTypeDto>
            {
                new ReportTypeDto { Id = "BookingSummary", Name = "Booking Summary", Description = "Summary of all bookings" },
                new ReportTypeDto { Id = "CancellationAnalysis", Name = "Cancellation Analysis", Description = "Analysis of cancellations" },
                new ReportTypeDto { Id = "DnaReport", Name = "DNA Report", Description = "Did Not Attend report" },
                new ReportTypeDto { Id = "SlotUtilisation", Name = "Slot Utilisation", Description = "Slot utilisation report" }
            };

            return Ok(types);
        }

        private async Task<ReportDataDto> GenerateReportDataAsync(GenerateReportRequestDto request)
        {
            var query = _context.Appointments
                .Include(a => a.Slot)
                .Include(a => a.Department)
                .Where(a => a.CreatedAt >= request.StartDate && a.CreatedAt <= request.EndDate)
                .AsQueryable();

            if (request.Filters?.DepartmentIds?.Any() == true)
            {
                query = query.Where(a => request.Filters.DepartmentIds.Contains(a.DepartmentId.ToString()));
            }

            var appointments = await query.ToListAsync();

            return new ReportDataDto
            {
                Summary = new ReportSummaryDto
                {
                    TotalBookings = appointments.Count,
                    TotalCancellations = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                    TotalDna = appointments.Count(a => a.Status == AppointmentStatus.DidNotAttend),
                    AverageUtilisation = 0
                },
                Details = appointments
                    .GroupBy(a => a.Department.Name)
                    .Select(g => new ReportDetailDto
                    {
                        Date = g.Max(a => a.CreatedAt),
                        Department = g.Key,
                        Bookings = g.Count(),
                        Cancellations = g.Count(a => a.Status == AppointmentStatus.Cancelled)
                    })
                    .ToList()
            };
        }

        private string GenerateCsvContent(ReportDataDto data)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Total Bookings,{data.Summary.TotalBookings}");
            csv.AppendLine($"Total Cancellations,{data.Summary.TotalCancellations}");
            csv.AppendLine($"Total DNA,{data.Summary.TotalDna}");
            csv.AppendLine($"Average Utilisation,{data.Summary.AverageUtilisation:F2}%");
            return csv.ToString();
        }
    }

    public class ReportTypeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}