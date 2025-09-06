using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EYEngage.Core.API.Controllers;

[ApiController]
[Authorize] // Authentification requise par défaut
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Récupère l'ID de l'utilisateur connecté
    /// </summary>
    /// <returns>GUID de l'utilisateur</returns>
    /// <exception cref="UnauthorizedAccessException">Si l'utilisateur n'est pas authentifié</exception>
    protected Guid GetCurrentUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Utilisateur non authentifié");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("ID utilisateur invalide ou manquant");

        return userId;
    }

    /// <summary>
    /// Récupère l'email de l'utilisateur connecté
    /// </summary>
    /// <returns>Email de l'utilisateur</returns>
    protected string GetCurrentUserEmail()
    {
        var email = User?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedAccessException("Email utilisateur manquant");

        return email;
    }

    /// <summary>
    /// Récupère les rôles de l'utilisateur connecté
    /// </summary>
    /// <returns>Liste des rôles</returns>
    protected List<string> GetCurrentUserRoles()
    {
        return User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }

    /// <summary>
    /// Vérifie si l'utilisateur a un rôle spécifique
    /// </summary>
    /// <param name="role">Nom du rôle</param>
    /// <returns>True si l'utilisateur a le rôle</returns>
    protected bool HasRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Vérifie si l'utilisateur a au moins un des rôles spécifiés
    /// </summary>
    /// <param name="roles">Liste des rôles à vérifier</param>
    /// <returns>True si l'utilisateur a au moins un des rôles</returns>
    protected bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => HasRole(role));
    }

    /// <summary>
    /// Récupère le département de l'utilisateur connecté
    /// </summary>
    /// <returns>Département de l'utilisateur ou null</returns>
    protected string? GetCurrentUserDepartment()
    {
        return User?.FindFirstValue("Department");
    }

    /// <summary>
    /// Vérifie si l'utilisateur est actif
    /// </summary>
    /// <returns>True si l'utilisateur est actif</returns>
    protected bool IsUserActive()
    {
        var isActiveClaim = User?.FindFirstValue("IsActive");
        return bool.TryParse(isActiveClaim, out var isActive) && isActive;
    }
}