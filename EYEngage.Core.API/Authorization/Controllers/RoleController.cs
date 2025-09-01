using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers
{
    [Route("api/roles")]
    [Authorize(Roles = "SuperAdmin")]
    public class RoleController : BaseController
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateRequestDto request)
        {
            var result = await _roleService.AddRoleAsync(request);
            return Ok(result);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignRequestDto request)
        {
            var result = await _roleService.AssignRoleToUserAsync(request);
            return Ok(result);
        }
    }
}