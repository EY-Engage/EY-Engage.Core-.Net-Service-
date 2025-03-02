namespace EYEngage.Core.Application.Dto.RoleDtos;

public record RoleCreateRequestDto
{
    public string RoleName { get; set; } = null!;
}
