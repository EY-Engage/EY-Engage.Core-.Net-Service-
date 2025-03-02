using EYEngage.Core.Application.Dto.AuthDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers;

/// <summary>
/// Contrôleur pour la gestion de l'authentification
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Enregistre un nouvel utilisateur
    /// </summary>
    /// <param name="request">Données d'inscription</param>
    /// <returns>Résultat de l'inscription</returns>
    /// <response code="200">Inscription réussie</response>
    /// <response code="400">Erreur de validation</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Connecte un utilisateur existant
    /// </summary>
    /// <param name="request">Données de connexion</param>
    /// <returns>Token JWT et informations utilisateur</returns>
    /// <response code="200">Connexion réussie</response>
    /// <response code="401">Identifiants invalides</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return result != null ? Ok(result) : Unauthorized();
    }
}