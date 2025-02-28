using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EYEngage.Core.API.Controllers;

    [ApiController]
    [Route("api/roles")]
    [Authorize(Roles = "SuperAdmin")]
    public class RoleController(IRoleService _roleService) : ControllerBase
    {
   

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
