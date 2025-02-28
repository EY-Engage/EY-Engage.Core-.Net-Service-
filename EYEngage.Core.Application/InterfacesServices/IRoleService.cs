using EYEngage.Core.Application.Dto.RoleDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IRoleService
{
    Task<RoleCreateResponseDto> AddRoleAsync(RoleCreateRequestDto request);
    Task<RoleAssignResponseDto> AssignRoleToUserAsync(RoleAssignRequestDto request);
}
