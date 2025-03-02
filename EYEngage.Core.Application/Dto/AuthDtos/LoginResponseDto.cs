namespace EYEngage.Core.Application.Dto.AuthDtos;

public record LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Email { get; internal set; }
    public List<string> Roles { get; internal set; }
}