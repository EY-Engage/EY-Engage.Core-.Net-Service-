using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.Dto;

public record CreateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public IFormFile? ProfilePictureFile { get; set; }
}
