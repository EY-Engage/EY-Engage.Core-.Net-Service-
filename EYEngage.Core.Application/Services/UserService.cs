using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Application.Services;

/// <summary>
/// Service de gestion des opérations CRUD pour les utilisateurs
/// </summary>
/// <remarks>
/// Ce service gère la création, lecture, mise à jour et suppression des utilisateurs,
/// ainsi que la gestion des images de profil.
/// </remarks>
public class UserService(UserManager<User> userManager, IWebHostEnvironment env) : IUserService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IWebHostEnvironment _env = env;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 Mo
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    /// <summary>
    /// Récupère la liste de tous les utilisateurs
    /// </summary>
    /// <returns>Liste des DTO utilisateurs</returns>
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        return await Task.FromResult(users.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Récupère un utilisateur par son identifiant
    /// </summary>
    /// <param name="userId">Identifiant unique de l'utilisateur</param>
    /// <returns>DTO de l'utilisateur</returns>
    /// <exception cref="NotFoundException">Si l'utilisateur n'existe pas</exception>
    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user == null
            ? throw new NotFoundException(nameof(User), userId)
            : MapToDto(user);
    }

    /// <summary>
    /// Crée un nouvel utilisateur
    /// </summary>
    /// <param name="dto">Données de création de l'utilisateur</param>
    /// <returns>DTO de l'utilisateur créé</returns>
    /// <exception cref="ValidationException">Si les données sont invalides</exception>
    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        ValidateUserInput(dto.Email, dto.Password, dto.ProfilePictureFile);

        var profilePicturePath = await SaveProfilePictureAsync(dto.ProfilePictureFile);

        var newUser = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            ProfilePicture = profilePicturePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var creationResult = await _userManager.CreateAsync(newUser, dto.Password);
        if (!creationResult.Succeeded)
        {
            throw new ValidationException($"Erreur de création : {GetErrors(creationResult)}");
        }

        await AddToDefaultRole(newUser);
        return MapToDto(newUser);
    }

    /// <summary>
    /// Met à jour un utilisateur existant
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <param name="dto">Données de mise à jour</param>
    /// <returns>DTO utilisateur mis à jour</returns>
    /// <exception cref="NotFoundException">Si l'utilisateur n'existe pas</exception>
    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        user.FullName = dto.FullName;
        user.UpdatedAt = DateTime.UtcNow;

        await HandleProfilePictureUpdate(user, dto.ProfilePictureFile);
        await HandlePasswordUpdate(user, dto.Password);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException($"Erreur de mise à jour : {GetErrors(updateResult)}");
        }

        return MapToDto(user);
    }

    /// <summary>
    /// Supprime un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>True si la suppression réussit</returns>
    /// <exception cref="NotFoundException">Si l'utilisateur n'existe pas</exception>
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        await DeleteProfilePicture(user.ProfilePicture);

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    #region Private Methods

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        ProfilePicture = user.ProfilePicture,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private async Task AddToDefaultRole(User user)
    {
        var roleResult = await _userManager.AddToRoleAsync(user, "EmployeeEY");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new ValidationException($"Erreur d'assignation de rôle : {GetErrors(roleResult)}");
        }
    }

    private async Task HandleProfilePictureUpdate(User user, IFormFile? newFile)
    {
        if (newFile == null) return;

        await DeleteProfilePicture(user.ProfilePicture);
        user.ProfilePicture = await SaveProfilePictureAsync(newFile);
    }

    private async Task HandlePasswordUpdate(User user, string? newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword)) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur de mot de passe : {GetErrors(result)}");
        }
    }

    private async Task<string> SaveProfilePictureAsync(IFormFile file)
    {
        ValidateFile(file);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folderPath = Path.Combine(_env.WebRootPath, "profilepictures");
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/profilepictures/{fileName}";
    }

    private async Task DeleteProfilePicture(string? picturePath)
    {
        if (string.IsNullOrEmpty(picturePath)) return;

        var fullPath = Path.Combine(_env.WebRootPath, picturePath.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
        }
    }

    private void ValidateUserInput(string email, string password, IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Email et mot de passe requis");
        }

        if (file != null) ValidateFile(file);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length > MaxFileSize)
        {
            throw new ValidationException($"Taille maximale autorisée : {MaxFileSize / 1024 / 1024} Mo");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ValidationException($"Extensions autorisées : {string.Join(", ", AllowedExtensions)}");
        }
    }

    private static string GetErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));

    #endregion
}