using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto.AuthDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EYEngage.Core.Application.Services;

/// <summary>
/// Service de gestion de l'authentification
/// </summary>
public class AuthService(UserManager<User> userManager, IConfiguration configuration) : IAuthService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Enregistre un nouvel utilisateur
    /// </summary>
    /// <param name="request">Données d'inscription</param>
    /// <returns>Message de succès</returns>
    /// <exception cref="ValidationException">Erreur de validation lors de l'inscription</exception>
    public async Task<string> RegisterAsync(RegisterRequestDto request)
    {
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ValidationException($"Échec de l'inscription : {errors}");
        }

        await _userManager.AddToRoleAsync(user, "EmployeeEY");
        return "Utilisateur créé avec succès";
    }

    /// <summary>
    /// Authentifie un utilisateur
    /// </summary>
    /// <param name="request">Données de connexion</param>
    /// <returns>Informations d'authentification</returns>
    /// <exception cref="UnauthorizedAccessException">Identifiants invalides</exception>
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Identifiants invalides");

        var roles = await _userManager.GetRolesAsync(user);
        var claims = GenerateClaims(user, roles);
        var token = GenerateJwtToken(claims);

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    private List<Claim> GenerateClaims(User user, IList<string> roles)
    {
        return new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        }.Union(roles.Select(role => new Claim(ClaimTypes.Role, role))).ToList();
    }

    private JwtSecurityToken GenerateJwtToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));

        return new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
    }
}