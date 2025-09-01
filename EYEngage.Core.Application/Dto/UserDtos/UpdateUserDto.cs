using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.Dto;

public record UpdateUserDto
{
    public string FullName { get; set; }
    public IFormFile? ProfilePictureFile { get; set; }
    public string Fonction { get; set; }
    public Department Department { get; set; }
    public string Sector { get; set; }
    public string PhoneNumber { get; set; }
}
