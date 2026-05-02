using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;

namespace HAMS.API.Services
{
    public class EhrIntegrationService : IEhrIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EhrIntegrationService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        private const string FHIR_BASE_URL = "https://ehr.mockserver.local/fhir";
        private static readonly Dictionary<string, MockPatientData> MockPatients = new()
        {
            ["1234567890"] = new MockPatientData
            {
                Id = Guid.NewGuid().ToString(),
                NhsNumber = "1234567890",
                FirstName = "John",
                LastName = "Smith",
                BirthDate = new DateTime(1985, 3, 15),
                Gender = "male",
                Address = "123 High Street, London, SW1A 1AA",
                Email = "john.smith@email.com",
                PhoneNumber = "07123456789"
            },
            ["0987654321"] = new MockPatientData
            {
                Id = Guid.NewGuid().ToString(),
                NhsNumber = "0987654321",
                FirstName = "Jane",
                LastName = "Doe",
                BirthDate = new DateTime(1990, 7, 22),
                Gender = "female",
                Address = "45 Oak Avenue, Manchester, M1 1AA",
                Email = "",
                PhoneNumber = "07987654321"
            }
        };

        public EhrIntegrationService(
            HttpClient httpClient,
            ILogger<EhrIntegrationService> logger,
            IDistributedCache cache,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _context = context;

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_configuration["Ehr:FhirBaseUrl"] ?? FHIR_BASE_URL);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/fhir+json");

            var authToken = _configuration["Ehr:AuthToken"];
            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            }
        }

        public async Task<PatientDemographicsDto?> GetPatientByNhsNumberAsync(string nhsNumber)
        {
            try
            {
                var cacheKey = $"patient_demographics_{nhsNumber}";
                var cached = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cached))
                {
                    _logger.LogInformation("Returning cached demographics for NHS number: {NhsNumber}", nhsNumber);
                    return JsonSerializer.Deserialize<PatientDemographicsDto>(cached) ?? null;
                }

                if (MockPatients.TryGetValue(nhsNumber, out var mockPatient))
                {
                    var mappedPatient = new PatientDemographicsDto
                    {
                        Id = mockPatient.Id,
                        NhsNumber = mockPatient.NhsNumber,
                        FirstName = mockPatient.FirstName,
                        LastName = mockPatient.LastName,
                        DateOfBirth = mockPatient.BirthDate,
                        Gender = mockPatient.Gender,
                        Email = mockPatient.Email,
                        PhoneNumber = mockPatient.PhoneNumber,
                        Address = mockPatient.Address,
                        LastUpdated = DateTime.UtcNow
                    };

                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(mappedPatient),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
                    return mappedPatient;
                }

                try
                {
                    var response = await _httpClient.GetAsync($"/Patient?identifier={nhsNumber}");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var patient = MapFhirPatientToDemographics(content);

                    if (patient != null)
                    {
                        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(patient),
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
                    }

                    return patient;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "EHR API unavailable, returning empty demographics");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient demographics for NHS number: {NhsNumber}", nhsNumber);
                return null;
            }
        }

        public async Task<MedicalHistoryDto?> GetMedicalHistoryAsync(string nhsNumber)
        {
            try
            {
                var cacheKey = $"medical_history_{nhsNumber}";
                var cached = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<MedicalHistoryDto>(cached) ?? null;
                }

                var mockData = GetMockMedicalData(nhsNumber);
                var history = new MedicalHistoryDto
                {
                    PatientId = mockData.PatientId,
                    NhsNumber = mockData.NhsNumber,
                    Diagnoses = mockData.Diagnoses,
                    ChronicConditions = mockData.ChronicConditions,
                    LastUpdated = DateTime.UtcNow
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(history),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medical history for NHS number: {NhsNumber}", nhsNumber);
                return null;
            }
        }

        public async Task<AllergiesDto?> GetAllergiesAsync(string nhsNumber)
        {
            try
            {
                var cacheKey = $"allergies_{nhsNumber}";
                var cached = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<AllergiesDto>(cached) ?? null;
                }

                var mockData = GetMockMedicalData(nhsNumber);
                var allergies = new AllergiesDto
                {
                    PatientId = mockData.PatientId,
                    NhsNumber = mockData.NhsNumber,
                    Allergies = mockData.Allergies,
                    LastUpdated = DateTime.UtcNow
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(allergies),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

                return allergies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving allergies for NHS number: {NhsNumber}", nhsNumber);
                return null;
            }
        }

        public async Task<MedicationsDto?> GetMedicationsAsync(string nhsNumber)
        {
            try
            {
                var cacheKey = $"medications_{nhsNumber}";
                var cached = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<MedicationsDto>(cached) ?? null;
                }

                var mockData = GetMockMedicalData(nhsNumber);
                var medications = new MedicationsDto
                {
                    PatientId = mockData.PatientId,
                    NhsNumber = mockData.NhsNumber,
                    Medications = mockData.Medications,
                    LastUpdated = DateTime.UtcNow
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(medications),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

                return medications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving medications for NHS number: {NhsNumber}", nhsNumber);
                return null;
            }
        }

        public async Task<SyncResultDto> SyncPatientDataAsync(string nhsNumber)
        {
            try
            {
                var demographics = await GetPatientByNhsNumberAsync(nhsNumber);

                if (demographics == null)
                {
                    return new SyncResultDto
                    {
                        Success = false,
                        Message = "Patient not found in EHR system",
                        SyncedAt = DateTime.UtcNow
                    };
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.NhsNumber == nhsNumber);

                if (patient != null)
                {
                    patient.User.Email = demographics.Email;
                    patient.User.PhoneNumber = demographics.PhoneNumber;
                    await _context.SaveChangesAsync();
                }

                return new SyncResultDto
                {
                    Success = true,
                    Message = $"Successfully synced data for patient {demographics.FirstName} {demographics.LastName}",
                    SyncedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing patient data for NHS number: {NhsNumber}", nhsNumber);
                return new SyncResultDto
                {
                    Success = false,
                    Message = "Failed to sync patient data",
                    SyncedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<HealthCheckDto> CheckServiceHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/metadata");
                var healthy = response.IsSuccessStatusCode;

                return new HealthCheckDto
                {
                    Healthy = healthy,
                    Message = healthy ? "EHR service is operational" : "EHR service is responding but returned non-OK status",
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EHR health check failed");
                return new HealthCheckDto
                {
                    Healthy = false,
                    Message = $"EHR service is unavailable: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        private PatientDemographicsDto? MapFhirPatientToDemographics(string fhirContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(fhirContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("entry", out var entries) && entries.GetArrayLength() > 0)
                {
                    var firstEntry = entries[0];
                    if (firstEntry.TryGetProperty("resource", out var resource))
                    {
                        var patient = new PatientDemographicsDto
                        {
                            Id = resource.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                            NhsNumber = ExtractIdentifier(resource, "NHS"),
                            FirstName = ExtractGivenName(resource),
                            LastName = ExtractFamilyName(resource),
                            LastUpdated = DateTime.UtcNow
                        };

                        return patient;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractGivenName(JsonElement resource)
        {
            if (resource.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
            {
                var name = names[0];
                if (name.TryGetProperty("given", out var given) && given.GetArrayLength() > 0)
                {
                    return given[0].GetString() ?? "";
                }
            }
            return "";
        }

        private string ExtractFamilyName(JsonElement resource)
        {
            if (resource.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
            {
                var name = names[0];
                if (name.TryGetProperty("family", out var family))
                {
                    return family.GetString() ?? "";
                }
            }
            return "";
        }

        private string ExtractIdentifier(JsonElement resource, string system)
        {
            if (resource.TryGetProperty("identifier", out var identifiers))
            {
                foreach (var id in identifiers.EnumerateArray())
                {
                    if (id.TryGetProperty("system", out var sys) && sys.GetString()?.Contains(system) == true)
                    {
                        if (id.TryGetProperty("value", out var value))
                            return value.GetString() ?? "";
                    }
                }
            }
            return "";
        }

        private MockMedicalData GetMockMedicalData(string nhsNumber)
        {
            return new MockMedicalData
            {
                PatientId = Guid.NewGuid().ToString(),
                NhsNumber = nhsNumber,
                Diagnoses = new List<DiagnosisDto>
                {
                    new DiagnosisDto { Code = "J06.9", Description = "Acute upper respiratory infection", DateRecorded = DateTime.UtcNow.AddMonths(-1), Status = "Resolved" }
                },
                ChronicConditions = new List<ConditionDto>(),
                Allergies = new List<AllergyDto>
                {
                    new AllergyDto { Substance = "Penicillin", Severity = "Severe", Reaction = "Anaphylaxis", OnsetDate = DateTime.UtcNow.AddYears(-10) }
                },
                Medications = new List<MedicationDto>
                {
                    new MedicationDto { Code = "037801", Name = "Amoxicillin", Dosage = "500mg", Frequency = "Three times daily", StartDate = DateTime.UtcNow.AddDays(-7), PrescribedBy = "GP Practice", IsActive = false },
                    new MedicationDto { Code = "010242", Name = "Paracetamol", Dosage = "1g", Frequency = "When required", StartDate = DateTime.UtcNow.AddMonths(-1), PrescribedBy = "GP Practice", IsActive = true }
                }
            };
        }

        private class MockPatientData
        {
            public string Id { get; set; } = "";
            public string NhsNumber { get; set; } = "";
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public DateTime BirthDate { get; set; }
            public string Gender { get; set; } = "";
            public string Address { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
        }

        private class MockMedicalData
        {
            public string PatientId { get; set; } = "";
            public string NhsNumber { get; set; } = "";
            public List<DiagnosisDto> Diagnoses { get; set; } = new();
            public List<ConditionDto> ChronicConditions { get; set; } = new();
            public List<AllergyDto> Allergies { get; set; } = new();
            public List<MedicationDto> Medications { get; set; } = new();
        }
    }
}