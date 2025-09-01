
using System.ComponentModel.DataAnnotations;


namespace EYEngage.Core.Application.Dto.AuthDtos;

public record ForgotPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; }
}

