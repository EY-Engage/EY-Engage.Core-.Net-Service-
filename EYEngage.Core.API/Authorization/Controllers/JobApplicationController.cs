using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class JobApplicationsController : BaseController
{
    private readonly IJobService _jobService;
    private readonly EYEngageDbContext _db;
    private readonly IFileStorageService _fileStorageService;

    public JobApplicationsController(
        IJobService jobService,
        EYEngageDbContext dbContext,
        IFileStorageService fileStorage)
    {
        _jobService = jobService;
        _db = dbContext;
        _fileStorageService = fileStorage;
    }

    // Accessible à tous les employés authentifiés
    [HttpPost("apply")]
    [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
    public async Task<IActionResult> Apply(
        [FromForm] JobApplicationDto dto,
        [FromForm] IFormFile? resume)
    {
        var userId = GetCurrentUserId();
        await _jobService.ApplyToJobAsync(dto, userId, resume);
        return Ok(new { message = "Candidature envoyée avec succès" });
    }

    // Accessible à tous les employés authentifiés
    [HttpPost("recommend")]
    [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
    public async Task<IActionResult> Recommend(
        [FromForm] JobApplicationDto dto,
        [FromForm] IFormFile resume)
    {
        var recommenderId = GetCurrentUserId();
        await _jobService.RecommendForJobAsync(dto, recommenderId, resume);
        return Ok(new { message = "Recommandation envoyée avec succès" });
    }

    // Admin et AgentEY seulement
    [HttpGet("for-job/{jobOfferId}")]
    [Authorize(Roles = "Admin,AgentEY")]
    public async Task<IActionResult> GetApplicationsForJob(Guid jobOfferId)
    {
        var applications = await _jobService.GetApplicationsForJobAsync(jobOfferId);
        return Ok(applications);
    }

    // Admin et AgentEY seulement
    [HttpGet("{applicationId}/resume")]
    [Authorize(Roles = "Admin,AgentEY")]
    public async Task<IActionResult> DownloadResume(Guid applicationId)
    {
        try
        {
            var stream = await _jobService.DownloadResumeAsync(applicationId);
            var application = await _db.JobApplications.FindAsync(applicationId);

            if (application == null)
                return NotFound("Candidature introuvable");

            return File(stream, "application/pdf",
                $"CV_{application.CandidateName.Replace(" ", "_")}.pdf");
        }
        catch (ValidationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
