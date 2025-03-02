using EYEngage.Core.Application.Dto.RoleDtos;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IRoleService
{
    Task<string> AddRoleAsync(RoleCreateRequestDto request);
    Task<string> AssignRoleToUserAsync(RoleAssignRequestDto request);
}
