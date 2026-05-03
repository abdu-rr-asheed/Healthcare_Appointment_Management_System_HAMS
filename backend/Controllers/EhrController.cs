using HAMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EhrController : ControllerBase
    {
        private readonly IEhrIntegrationService _ehrService;
        private readonly ILogger<EhrController> _logger;

        public EhrController(
            IEhrIntegrationService ehrService,
            ILogger<EhrController> logger)
        {
            _ehrService = ehrService;
            _logger = logger;
        }

        [HttpGet("patient/{nhsNumber}")]
        [Authorize(Roles = "Patient,Clinician,Administrator")]
        public async Task<IActionResult> GetPatient(string nhsNumber)
        {
            if (string.IsNullOrWhiteSpace(nhsNumber) || nhsNumber.Length != 10)
            {
                return BadRequest(new { message = "Invalid NHS number. Must be 10 digits." });
            }

            try
            {
                var patient = await _ehrService.GetPatientByNhsNumberAsync(nhsNumber);
                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found in EHR system." });
                }
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient {NhsNumber} from EHR", nhsNumber);
                return StatusCode(503, new { message = "EHR service unavailable." });
            }
        }

        [HttpGet("patient/{nhsNumber}/medical-history")]
        [Authorize(Roles = "Clinician,Administrator")]
        public async Task<IActionResult> GetMedicalHistory(string nhsNumber)
        {
            if (string.IsNullOrWhiteSpace(nhsNumber) || nhsNumber.Length != 10)
            {
                return BadRequest(new { message = "Invalid NHS number." });
            }

            try
            {
                var history = await _ehrService.GetMedicalHistoryAsync(nhsNumber);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medical history for {NhsNumber}", nhsNumber);
                return StatusCode(503, new { message = "EHR service unavailable." });
            }
        }

        [HttpGet("patient/{nhsNumber}/allergies")]
        [Authorize(Roles = "Patient,Clinician,Administrator")]
        public async Task<IActionResult> GetAllergies(string nhsNumber)
        {
            if (string.IsNullOrWhiteSpace(nhsNumber) || nhsNumber.Length != 10)
            {
                return BadRequest(new { message = "Invalid NHS number." });
            }

            try
            {
                var allergies = await _ehrService.GetAllergiesAsync(nhsNumber);
                return Ok(allergies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching allergies for {NhsNumber}", nhsNumber);
                return StatusCode(503, new { message = "EHR service unavailable." });
            }
        }

        [HttpGet("patient/{nhsNumber}/medications")]
        [Authorize(Roles = "Patient,Clinician,Administrator")]
        public async Task<IActionResult> GetMedications(string nhsNumber)
        {
            if (string.IsNullOrWhiteSpace(nhsNumber) || nhsNumber.Length != 10)
            {
                return BadRequest(new { message = "Invalid NHS number." });
            }

            try
            {
                var medications = await _ehrService.GetMedicationsAsync(nhsNumber);
                return Ok(medications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medications for {NhsNumber}", nhsNumber);
                return StatusCode(503, new { message = "EHR service unavailable." });
            }
        }

        [HttpPost("patient/sync")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> SyncPatient([FromBody] SyncPatientRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NhsNumber) || request.NhsNumber.Length != 10)
            {
                return BadRequest(new { message = "Invalid NHS number." });
            }

            try
            {
                var result = await _ehrService.SyncPatientDataAsync(request.NhsNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing patient {NhsNumber}", request.NhsNumber);
                return StatusCode(503, new { message = "EHR sync failed." });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEhrHealth()
        {
            var health = await _ehrService.CheckServiceHealthAsync();
            return Ok(health);
        }
    }

    public class SyncPatientRequest
    {
        public string NhsNumber { get; set; } = string.Empty;
    }
}