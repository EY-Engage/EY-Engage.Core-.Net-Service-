using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.Dto;

public record UpdateUserDto
{
    public string FullName { get; set; }
    public IFormFile? ProfilePictureFile { get; set; }
    public string? Password { get; set; }
}
