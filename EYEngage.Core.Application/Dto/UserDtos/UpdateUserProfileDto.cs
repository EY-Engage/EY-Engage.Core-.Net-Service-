using EYEngage.Core.Domain;
using System.ComponentModel.DataAnnotations;


namespace EYEngage.Core.Application.Dto.UserDtos;

public record UpdateUserProfileDto
{
    [Required]
    public string FullName { get; set; }

    public string PhoneNumber { get; set; }

    [Required]
    public string Fonction { get; set; }

    [Required]
    public Department Department { get; set; }

    [Required]
    public string Sector { get; set; }
}
