using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Application.Services;

/// <summary>
/// Service de gestion des rôles utilisateurs
/// </summary>
public class RoleService(RoleManager<Role> roleManager, UserManager<User> userManager) : IRoleService
{
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly UserManager<User> _userManager = userManager;

    /// <summary>
    /// Ajoute un nouveau rôle au système
    /// </summary>
    /// <param name="request">Requête de création de rôle</param>
    /// <returns>Message de confirmation</returns>
    /// <exception cref="ValidationException">Si le rôle existe déjà</exception>
    public async Task<string> AddRoleAsync(RoleCreateRequestDto request)
    {
        if (await _roleManager.RoleExistsAsync(request.RoleName))
        {
            throw new ValidationException($"Le rôle '{request.RoleName}' existe déjà");
        }

        var role = new Role
        {
            Name = request.RoleName,
            NormalizedName = request.RoleName.ToUpper(),
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur de création : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return $"Rôle '{request.RoleName}' créé avec succès";
    }

    /// <summary>
    /// Assigner un rôle à un utilisateur
    /// </summary>
    /// <param name="request">Requête d'assignation</param>
    /// <returns>Message de confirmation</returns>
    /// <exception cref="NotFoundException">Utilisateur ou rôle introuvable</exception>
    /// <exception cref="ValidationException">Erreur d'assignation</exception>
    public async Task<string> AssignRoleToUserAsync(RoleAssignRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            throw new NotFoundException(nameof(Role), request.RoleName);
        }

        if (await _userManager.IsInRoleAsync(user, request.RoleName))
        {
            throw new ValidationException($"L'utilisateur possède déjà le rôle '{request.RoleName}'");
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur d'assignation : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return $"Rôle '{request.RoleName}' assigné avec succès à {user.Email}";
    }
}