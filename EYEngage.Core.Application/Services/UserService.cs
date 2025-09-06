using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.UserDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;
    private readonly EYEngageDbContext _db;
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    public UserService(UserManager<User> userManager, IWebHostEnvironment env, IEmailService emailService, EYEngageDbContext db)
    {
        _userManager = userManager;
        _env = env;
        _emailService = emailService;
        _db = db;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);
        return MapToDto(user);
    }

    // NOUVELLE MÉTHODE: Obtenir le profil public d'un utilisateur
    public async Task<UserPublicProfileDto> GetUserPublicProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        var roles = await _userManager.GetRolesAsync(user);

        return new UserPublicProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ProfilePicture = user.ProfilePicture,
            PhoneNumber = user.PhoneNumber,
            Fonction = user.Fonction,
            Department = user.Department,
            Sector = user.Sector,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive,
            Roles = roles?.ToList()
        };
    }

    public async Task<string> CreateUserAsync(CreateUserDto dto)
    {
        ValidateUserInput(dto.Email, dto.Password, dto.ProfilePictureFile);
        var picturePath = await SaveProfilePictureAsync(dto.ProfilePictureFile);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            ProfilePicture = picturePath,
            PhoneNumber = dto.PhoneNumber,
            Fonction = dto.Fonction,
            Department = dto.Department,
            Sector = dto.Sector,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = false,
            IsFirstLogin = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) throw new ValidationException($"Création échouée : {GetErrors(result)}");

        await AddToDefaultRole(user);


        await _emailService.SendUserCredentials(user.Email, dto.Password);
        return "Utilisateur créé avec succès.";
    }

    public async Task<string> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Fonction = dto.Fonction;
        user.Department = dto.Department;
        user.Sector = dto.Sector;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur de mise à jour : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return "Profil mis à jour avec succès";
    }

    public async Task<string> UpdateUserProfilePictureAsync(Guid userId, IFormFile profilePicture)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        if (profilePicture == null || profilePicture.Length == 0)
            throw new ValidationException("Aucune image fournie");

        // Supprimer l'ancienne photo si elle existe
        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            await DeleteProfilePicture(user.ProfilePicture);
        }

        // Sauvegarder la nouvelle photo
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(profilePicture.FileName)}";
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-pictures", fileName);

        var directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(stream);
        }

        user.ProfilePicture = $"/profile-pictures/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur de mise à jour : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return "Photo de profil mise à jour avec succès";
    }

    public async Task<string> UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), userId);

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
        {
            throw new ValidationException($"Erreur de changement de mot de passe : {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return "Mot de passe mis à jour avec succès";
    }

    public async Task<string> UpdateUserAsync(Guid userId, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        user.FullName = dto.FullName;
        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Fonction)) user.Fonction = dto.Fonction;
        user.Department = dto.Department;
        if (!string.IsNullOrWhiteSpace(dto.Sector)) user.Sector = dto.Sector;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;

        if (dto.ProfilePictureFile != null)
        {
            await DeleteProfilePicture(user.ProfilePicture);
            user.ProfilePicture = await SaveProfilePictureAsync(dto.ProfilePictureFile);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new ValidationException($"Mise à jour échouée : {GetErrors(result)}");

        return "Utilisateur mis à jour avec succès.";
    }

    public async Task<string> DeleteUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        await DeleteUserDependencies(userId);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return "Utilisateur supprimé avec succès.";
    }

    private async Task DeleteUserDependencies(Guid userId)
    {
        // 0. Supprimer les événements organisés par l'utilisateur
        var organizedEvents = await _db.Events
            .Where(e => e.OrganizerId == userId)
            .ToListAsync();
        _db.Events.RemoveRange(organizedEvents);

        // 1. Supprimer participations à des événements
        var participations = await _db.EventParticipations
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _db.EventParticipations.RemoveRange(participations);

        // 2. Supprimer intérêts d'événements
        var interests = await _db.EventInterests
            .Where(i => i.UserId == userId)
            .ToListAsync();
        _db.EventInterests.RemoveRange(interests);

        // 3. Supprimer commentaires et leurs dépendances
        var comments = await _db.Comments
            .Where(c => c.AuthorId == userId)
            .Include(c => c.Reactions)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Reactions)
            .ToListAsync();

        foreach (var comment in comments)
        {
            foreach (var reply in comment.Replies)
            {
                _db.CommentReplyReactions.RemoveRange(reply.Reactions);
            }
            _db.CommentReactions.RemoveRange(comment.Reactions);
            _db.CommentReplies.RemoveRange(comment.Replies);
        }
        _db.Comments.RemoveRange(comments);

        // 4. Supprimer les offres d'emploi publiées
        var jobOffers = await _db.JobOffers
            .Where(j => j.PublisherId == userId)
            .ToListAsync();
        _db.JobOffers.RemoveRange(jobOffers);

        // 5. Supprimer les candidatures associées
        var jobApplications = await _db.JobApplications
            .Where(a => a.UserId == userId || a.RecommendedByUserId == userId)
            .ToListAsync();
        _db.JobApplications.RemoveRange(jobApplications);

        // 6. Supprimer les rôles de l'utilisateur
        var userRoles = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
        _db.UserRoles.RemoveRange(userRoles);

        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            throw new ValidationException("Email invalide ou manquant");

        return !await _db.Users.AnyAsync(u => u.Email == email);
    }

    #region --- Helpers ---

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        ProfilePicture = u.ProfilePicture,
        PhoneNumber = u.PhoneNumber,
        Fonction = u.Fonction,
        Department = u.Department,
        Sector = u.Sector,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        IsActive = u.IsActive
    };

    private async Task AddToDefaultRole(User u)
    {
        var result = await _userManager.AddToRoleAsync(u, "EmployeeEY");
        if (!result.Succeeded)
        {
            await _userManager.DeleteAsync(u);
            throw new ValidationException($"Affectation rôle échouée : {GetErrors(result)}");
        }
    }

    private void ValidateUserInput(string mail, string pwd, IFormFile? f)
    {
        if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(pwd))
            throw new ValidationException("Email & mot de passe obligatoires");
        if (f != null) ValidateFile(f);
    }

    private void ValidateFile(IFormFile f)
    {
        if (f.Length > MaxFileSize)
            throw new ValidationException($"Taille max : {MaxFileSize / 1024 / 1024} Mo");
        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ValidationException($"Extensions autorisées : {string.Join(", ", AllowedExtensions)}");
    }

    private async Task<string> SaveProfilePictureAsync(IFormFile file)
    {
        if (file == null || file.Length == 0) return string.Empty;
        ValidateFile(file);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folder = Path.Combine(_env.WebRootPath, "profilepictures");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);

        await using var fs = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(fs);

        return $"/profilepictures/{fileName}";
    }

    private async Task DeleteProfilePicture(string? picture)
    {
        if (string.IsNullOrWhiteSpace(picture)) return;
        var full = Path.Combine(_env.WebRootPath, picture.TrimStart('/'));
        if (File.Exists(full)) await Task.Run(() => File.Delete(full));
    }

    private static string GetErrors(IdentityResult res) =>
        string.Join(", ", res.Errors.Select(x => x.Description));

    #endregion
}