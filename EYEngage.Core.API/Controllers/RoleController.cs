using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers;

/// <summary>
/// Contrôleur pour la gestion des rôles utilisateurs
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize(Policy = "SuperAdminPolicy")]
public class RoleController(IRoleService roleService) : ControllerBase
{
    private readonly IRoleService _roleService = roleService;

    /// <summary>
    /// Crée un nouveau rôle dans le système
    /// </summary>
    /// <param name="request">Données de création du rôle</param>
    /// <returns>Confirmation de création</returns>
    /// <response code="200">Rôle créé avec succès</response>
    /// <response code="400">Erreur de validation</response>
    /// <response code="401">Non autorisé</response>
    /// <response code="409">Le rôle existe déjà</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateRequestDto request)
    {
        var result = await _roleService.AddRoleAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Assigner un rôle à un utilisateur
    /// </summary>
    /// <param name="request">Données d'assignation</param>
    /// <returns>Confirmation d'assignation</returns>
    /// <response code="200">Rôle assigné avec succès</response>
    /// <response code="400">Erreur de validation</response>
    /// <response code="404">Utilisateur ou rôle introuvable</response>
    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignRequestDto request)
    {
        var result = await _roleService.AssignRoleToUserAsync(request);
        return Ok(result);
    }
}