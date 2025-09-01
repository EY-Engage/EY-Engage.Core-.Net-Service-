using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = "Admin,AgentEY")]
public class JobRecommendationsController : BaseController
{
    private readonly IJobService _jobService;

    public JobRecommendationsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    // Admin et AgentEY seulement
    [HttpGet("{jobOfferId}/top-candidates")]

    public async Task<IActionResult> GetTopCandidates(Guid jobOfferId)
    {
        var candidates = await _jobService.GetTopCandidatesAsync(jobOfferId);
        return Ok(candidates);
    }

    // Admin et AgentEY seulement
    [HttpPost("schedule-interview")]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Requête invalide");

            if (request.ApplicationId == Guid.Empty)
                return BadRequest("ID de candidature invalide");

            if (string.IsNullOrWhiteSpace(request.Location))
                return BadRequest("L'emplacement est requis");

            if (request.InterviewDate <= DateTime.Now)
                return BadRequest("La date d'entretien doit être dans le futur");

            await _jobService.ScheduleInterviewAsync(
                request.ApplicationId,
                request.InterviewDate,
                request.Location
            );

            return Ok(new { message = "Entretien programmé avec succès" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Une erreur s'est produite lors de la programmation de l'entretien" });
        }
    }
}