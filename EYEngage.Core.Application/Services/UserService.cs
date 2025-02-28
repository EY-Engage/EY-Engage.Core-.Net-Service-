using EYEngage.Core.Application.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Hosting;
using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Services;

public class UserService(UserManager<User> _userManager, IWebHostEnvironment _env) : IUserService
{
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            ProfilePicture = u.ProfilePicture,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        string? picturePath = null;
        if (dto.ProfilePictureFile != null)
        {
            picturePath = await SaveProfilePictureAsync(dto.ProfilePictureFile);
        }

        var newUser = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            ProfilePicture = picturePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Erreur lors de la création de l'utilisateur: {errors}");
        }
        await _userManager.AddToRoleAsync(newUser, "EmployeeEY");
        return new UserDto
        {
            Id = newUser.Id,
            FullName = newUser.FullName,
            Email = newUser.Email,
            ProfilePicture = newUser.ProfilePicture,
            CreatedAt = newUser.CreatedAt,
            UpdatedAt = newUser.UpdatedAt
        };
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        user.FullName = dto.FullName;
        if (dto.ProfilePictureFile != null)
        {
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                var oldImagePath = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/'));
                if (File.Exists(oldImagePath))
                {
                    File.Delete(oldImagePath);
                }
            }
            user.ProfilePicture = await SaveProfilePictureAsync(dto.ProfilePictureFile);
        }

        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.Password);
            if (!passwordResult.Succeeded)
            {
                var errors = string.Join("; ", passwordResult.Errors.Select(e => e.Description));
                throw new Exception($"Erreur lors de la mise à jour du mot de passe : {errors}");
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new Exception($"Erreur lors de la mise à jour de l'utilisateur : {errors}");
        }

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }


    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    private async Task<string> SaveProfilePictureAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Le fichier d'image est invalide.");
        }

        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ArgumentException("Format d'image non pris en charge. Veuillez utiliser JPG, PNG ou GIF.");
        }

        var folderPath = Path.Combine(_env.WebRootPath, "profilepictures");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folderPath, fileName);

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Erreur lors de l'enregistrement de l'image de profil.", ex);
        }

        return $"/profilepictures/{fileName}";
        //+++
    }

}
