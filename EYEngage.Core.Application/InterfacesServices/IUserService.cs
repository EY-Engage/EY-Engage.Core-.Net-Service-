using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.UserDtos;
using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid userId);
    Task<string> CreateUserAsync(CreateUserDto dto);
    Task<string> UpdateUserAsync(Guid userId, UpdateUserDto dto);
    Task<string> DeleteUserAsync(Guid userId);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<string> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto dto);
    Task<string> UpdatePasswordAsync(Guid userId, UpdatePasswordDto dto);
    Task<string> UpdateUserProfilePictureAsync(Guid userId, IFormFile profilePicture);
    Task<UserPublicProfileDto> GetUserPublicProfileAsync(Guid userId);
}