using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Dto.AuthDtos;

public record TokenResponseDto
{
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public Guid? SessionId { get; set; }
}