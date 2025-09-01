

using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Application.Dto.UserDtos;

public record UpdatePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; }
}
