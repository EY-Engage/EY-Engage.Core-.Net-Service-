using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.UserDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize] // Require authentication by default
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // SuperAdmin seulement
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // SuperAdmin seulement
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(user);
        }

        // NOUVEAU: Endpoint pour obtenir le profil public d'un utilisateur
        [HttpGet("public/{id}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetUserPublicProfile(Guid id)
        {
            var user = await _userService.GetUserPublicProfileAsync(id);
            return Ok(user);
        }

        // SuperAdmin seulement
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserDto dto)
        {
            var message = await _userService.CreateUserAsync(dto);
            return Ok(new { message });
        }

        // SuperAdmin seulement
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromForm] UpdateUserDto dto)
        {
            var message = await _userService.UpdateUserAsync(id, dto);
            if (string.IsNullOrEmpty(message))
                return NotFound("Utilisateur introuvable");

            return Ok(new { message });
        }

        // SuperAdmin seulement
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var message = await _userService.DeleteUserAsync(id);
            if (string.IsNullOrEmpty(message))
                return NotFound("Utilisateur introuvable ou erreur lors de la suppression");

            return Ok(new { message });
        }

        // SuperAdmin seulement
        [HttpGet("check-email")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CheckEmailUnique([FromQuery] string email)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Requête invalide" });

                var isUnique = await _userService.IsEmailUniqueAsync(email);
                return Ok(new { isUnique });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpPut("profile-picture")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            var userId = GetCurrentUserId();
            var message = await _userService.UpdateUserProfilePictureAsync(userId, profilePicture);
            return Ok(new { message });
        }

        // IMPORTANT: Accessible à TOUS les utilisateurs authentifiés pour leur propre profil
        [HttpGet("profile")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var userDto = await _userService.GetUserByIdAsync(userId);
            return Ok(userDto);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var message = await _userService.UpdateUserProfileAsync(userId, dto);
            return Ok(new { message });
        }

        [HttpPost("change-password")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> ChangePassword([FromBody] UpdatePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            var message = await _userService.UpdatePasswordAsync(userId, dto);
            return Ok(new { message });
        }
    }
}