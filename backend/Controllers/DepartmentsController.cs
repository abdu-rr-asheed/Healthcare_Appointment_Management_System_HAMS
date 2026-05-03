using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HAMS.API.Data;
using HAMS.API.Models.DTOs.Responses;
using HAMS.API.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HAMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(ApplicationDbContext context, ILogger<DepartmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DepartmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                var result = departments.Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get departments");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred while fetching departments" });
            }
        }

        [HttpGet("{id}/clinicians")]
        [ProducesResponseType(typeof(IEnumerable<ClinicianDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCliniciansByDepartment(Guid id)
        {
            try
            {
                var clinicians = await _context.Clinicians
                    .Include(c => c.User)
                    .Include(c => c.Department)
                    .Where(c => c.DepartmentId == id && c.Status == ClinicianStatus.Active && c.User.IsActive)
                    .OrderBy(c => c.User.LastName)
                    .ThenBy(c => c.User.FirstName)
                    .ToListAsync();

                var result = clinicians.Select(c => new ClinicianDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    FirstName = c.User.FirstName,
                    LastName = c.User.LastName,
                    Specialty = c.Specialty,
                    LicenseNumber = c.LicenseNumber,
                    Qualifications = c.Qualifications,
                    Status = c.Status.ToString()
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clinicians for department");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred while fetching clinicians" });
            }
        }
    }
}