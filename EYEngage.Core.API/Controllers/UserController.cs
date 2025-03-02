using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers;

/// <summary>
/// Contrôleur pour la gestion des utilisateurs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminPolicy")]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Récupère tous les utilisateurs
    /// </summary>
    /// <returns>Liste des utilisateurs</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Récupère un utilisateur par son ID
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur</param>
    /// <returns>Détails de l'utilisateur</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    /// <summary>
    /// Ajout D'un Nouvel Utilisateur
    /// </summary>
    /// <returns>Détails de l'utilisateur crée</returns>
    [HttpPost]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserDto dto)
        {
            var createdUser = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

    /// <summary>
    /// Modifie un utilisateur par son ID
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur</param>
    /// <returns>Détails de l'utilisateur modifié</returns>
    [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromForm] UpdateUserDto dto)
        {
            var updatedUser = await _userService.UpdateUserAsync(id, dto);
            if (updatedUser == null)
                return NotFound("Utilisateur introuvable");

            return Ok(updatedUser);
        }

    /// <summary>
    /// Supprime un utilisateur par son ID
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur</param>
    /// <returns>Un message de succés lors de la suppresion</returns>
    [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
                return NotFound("Utilisateur introuvable ou erreur lors de la suppression");

            return Ok("Utilisateur supprimé avec succès");
        }
    }
