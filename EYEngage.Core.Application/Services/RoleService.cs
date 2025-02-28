using EYEngage.Core.Application.Dto.RoleDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleService(RoleManager<Role> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<RoleCreateResponseDto> AddRoleAsync(RoleCreateRequestDto request)
        {
            if (await _roleManager.RoleExistsAsync(request.RoleName))
            {
                return new RoleCreateResponseDto
                {
                    Success = false,
                    Message = "Le rôle existe déjà."
                };
            }

            var role = new Role { Name = request.RoleName, NormalizedName = request.RoleName.ToUpper() };
            var result = await _roleManager.CreateAsync(role);
            return new RoleCreateResponseDto
            {
                Success = result.Succeeded,
                Message = result.Succeeded ? "Rôle créé avec succès." : "Erreur lors de la création du rôle."
            };
        }

        public async Task<RoleAssignResponseDto> AssignRoleToUserAsync(RoleAssignRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return new RoleAssignResponseDto { Success = false, Message = "Utilisateur introuvable." };
            }

            if (!await _roleManager.RoleExistsAsync(request.RoleName))
            {
                return new RoleAssignResponseDto { Success = false, Message = "Le rôle n'existe pas." };
            }

            var result = await _userManager.AddToRoleAsync(user, request.RoleName);
            return new RoleAssignResponseDto
            {
                Success = result.Succeeded,
                Message = result.Succeeded ? "Rôle assigné avec succès." : "Erreur lors de l'assignation du rôle."
            };
        }
    }
}
