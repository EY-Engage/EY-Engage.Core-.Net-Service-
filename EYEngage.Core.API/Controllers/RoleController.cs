using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateRequestDto request)
        {
            var response = await _roleService.AddRoleAsync(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignRequestDto request)
        {
            var response = await _roleService.AssignRoleToUserAsync(request);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
