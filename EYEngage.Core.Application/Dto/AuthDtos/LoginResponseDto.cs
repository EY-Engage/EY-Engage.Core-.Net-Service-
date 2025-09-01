using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Dto.AuthDtos;

public record LoginResponseDto
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public int ExpiresIn { get; init; }
    public bool IsFirstLogin { get; init; }
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = new();
    public string? Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? SessionId { get; set; }
}