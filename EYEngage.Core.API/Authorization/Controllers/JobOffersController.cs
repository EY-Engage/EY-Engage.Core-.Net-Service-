using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EYEngage.Core.Domain;

namespace EYEngage.Core.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class JobOffersController : BaseController
    {
        private readonly IJobService _jobService;
        private readonly UserManager<User> _userManager;

        public JobOffersController(
            IJobService jobService,
            UserManager<User> userManager)
        {
            _jobService = jobService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> Get([FromQuery] string? department)
        {
            string? departmentFilter = null;

            if (User.IsInRole("AgentEY"))
            {
                var user = await _userManager.GetUserAsync(User);
                departmentFilter = user?.Department.ToString();
            }
            else if (!string.IsNullOrEmpty(department))
            {
                departmentFilter = department;
            }

            var jobs = await _jobService.GetJobOffersAsync(departmentFilter);
            return Ok(jobs);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var job = await _jobService.GetJobOfferByIdAsync(id);
            return Ok(job);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> Create([FromBody] JobOfferDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null) return Unauthorized();

            var department = user.Department;
            var job = await _jobService.CreateJobOfferAsync(dto, userId, department);
            return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _jobService.DeleteJobOfferAsync(id);
            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> Update(Guid id, [FromBody] JobOfferDto dto)
        {
            if (id != dto.Id)
                return BadRequest("L'ID de la route ne correspond pas à l'ID de l'offre d'emploi");

            await _jobService.UpdateJobOfferAsync(dto);
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
                throw new UnauthorizedAccessException("Invalid or missing user ID.");

            return guid;
        }
    }
}