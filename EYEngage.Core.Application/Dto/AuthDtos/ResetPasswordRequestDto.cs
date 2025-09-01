
using System.ComponentModel.DataAnnotations;


namespace EYEngage.Core.Application.Dto.AuthDtos;

public record ResetPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Token { get; set; }

    [Required, MinLength(6)]
    public string NewPassword { get; set; }
}
