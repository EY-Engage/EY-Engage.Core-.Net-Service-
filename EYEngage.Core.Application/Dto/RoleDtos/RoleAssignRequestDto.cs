namespace EYEngage.Core.Application.Dto.RoleDtos;

public record RoleAssignRequestDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!;
}
