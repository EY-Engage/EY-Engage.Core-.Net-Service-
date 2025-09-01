using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Application.Dto.AuthDtos;

public record ChangePasswordDto
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; }

    [Required]
    public string ConfirmNewPassword { get; set; }
}

