using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.AuthDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EYEngage.Core.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _um;
        private readonly IConfiguration _cfg;
        private readonly IHttpContextAccessor _ctx;
        private readonly SocialNotificationService _socialNotificationService;
        public AuthService(
            UserManager<User> userManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            SocialNotificationService socialNotificationService)
        {
            _um = userManager;
            _cfg = configuration;
            _ctx = httpContextAccessor;
            _socialNotificationService = socialNotificationService;
        }

        public async Task<string> RegisterAsync(RegisterRequestDto req)
        {
            var user = new User
            {
                FullName = req.FullName,
                Email = req.Email,
                UserName = req.Email,
                IsActive = false,
                IsFirstLogin = true
            };

            var result = await _um.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _um.AddToRoleAsync(user, "EmployeeEY");

            var emailService = _ctx.HttpContext!
                .RequestServices
                .GetRequiredService<IEmailService>();
            await emailService.SendUserCredentials(user.Email, req.Password);

            return "Utilisateur créé, email envoyé.";
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto req)
        {
            var user = await _um.FindByEmailAsync(req.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Identifiants invalides");
            }

            if (!await _um.CheckPasswordAsync(user, req.Password))
            {
                throw new UnauthorizedAccessException("Identifiants invalides");
            }

            var roles = (await _um.GetRolesAsync(user)).ToList();

            // SuperAdmin bypass
            if (roles.Contains("SuperAdmin"))
            {
                user.IsActive = true;
                user.IsFirstLogin = false;
            }

            // NOTIFICATION: Utilisateur activé (si premier login réussi)
            if (user.IsActive && !user.IsFirstLogin)
            {
                await _socialNotificationService.NotifyUserActivated(user);
            }

            // Générer un nouvel ID de session à chaque login
            user.SessionId = Guid.NewGuid();
            await _um.UpdateAsync(user);

            // Si l'utilisateur doit changer son mot de passe
            if (!user.IsActive || user.IsFirstLogin)
            {
                return new LoginResponseDto
                {
                    IsActive = user.IsActive,
                    IsFirstLogin = user.IsFirstLogin,
                    Roles = roles,
                    Id = user.Id.ToString(),
                    FullName = user.FullName,
                    Email = user.Email
                };
            }

            // Générer les tokens
            var accessToken = GenerateAccessToken(user, roles);
            var refreshToken = GenerateRefreshToken();
            await UpdateRefreshToken(user, refreshToken, TimeSpan.FromDays(7));

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)TimeSpan.FromMinutes(15).TotalSeconds,
                IsActive = true,
                IsFirstLogin = false,
                Roles = roles,
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email
            };
        }

        public async Task<LoginResponseDto> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _um.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new ValidationException("Utilisateur non trouvé");
            }

            if (!await _um.CheckPasswordAsync(user, dto.CurrentPassword))
            {
                throw new ValidationException("Mot de passe actuel incorrect");
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                throw new ValidationException("Les nouveaux mots de passe ne correspondent pas");
            }

            var changeResult = await _um.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!changeResult.Succeeded)
            {
                throw new ValidationException($"Échec du changement : {string.Join(", ", changeResult.Errors.Select(e => e.Description))}");
            }

            // Mettre à jour les flags
            user.IsActive = true;
            user.IsFirstLogin = false;
            user.SessionId = Guid.NewGuid();

            await _um.UpdateSecurityStampAsync(user);
            await _um.UpdateAsync(user);

            // NOTIFICATION: Utilisateur activé (après changement de mot de passe)
            await _socialNotificationService.NotifyUserActivated(user);

            var roles = (await _um.GetRolesAsync(user)).ToList();
            var newAccess = GenerateAccessToken(user, roles);
            var newRefresh = GenerateRefreshToken();
            await UpdateRefreshToken(user, newRefresh, TimeSpan.FromDays(7));

            return new LoginResponseDto
            {
                AccessToken = newAccess,
                RefreshToken = newRefresh,
                ExpiresIn = (int)TimeSpan.FromMinutes(15).TotalSeconds,
                IsActive = true,
                IsFirstLogin = false,
                Roles = roles,
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email
            };
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new SecurityTokenException("Refresh token manquant");
            }

            var user = await _um.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                throw new SecurityTokenException("Refresh token invalide ou expiré");
            }

            // Vérification du statut utilisateur
            if (!user.IsActive || user.IsFirstLogin)
            {
                throw new UnauthorizedAccessException("L'utilisateur doit changer son mot de passe");
            }
            user.SessionId = Guid.NewGuid(); // <-- AJOUTEZ CETTE LIGNE
            await _um.UpdateAsync(user);
            var roles = (await _um.GetRolesAsync(user)).ToList();
            var newAccessToken = GenerateAccessToken(user, roles);
            var newRefreshToken = GenerateRefreshToken();

            // Mettre à jour le refresh token
            await UpdateRefreshToken(user, newRefreshToken, TimeSpan.FromDays(7));

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = (int)TimeSpan.FromMinutes(15).TotalSeconds
            };
        }

        public async Task LogoutAsync()
        {
            try
            {
                var user = await _um.GetUserAsync(_ctx.HttpContext!.User);
                if (user != null)
                {
                    user.SessionId = null;
                    user.RefreshToken = null;
                    user.RefreshTokenExpiry = null;
                    await _um.UpdateAsync(user);
                }
            }
            catch (Exception)
            {
                // Ignorer les erreurs de logout
            }
        }

        public async Task<ValidateResponseDto> ValidateAsync()
        {
            var user = await _um.GetUserAsync(_ctx.HttpContext!.User);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non trouvé");
            }

            // Vérifier que la session est toujours valide
            if (user.SessionId == null)
            {
                throw new UnauthorizedAccessException("Session invalide");
            }

            var roles = (await _um.GetRolesAsync(user)).ToList();

            return new ValidateResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles,
                IsActive = user.IsActive,
                IsFirstLogin = user.IsFirstLogin
            };
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _um.FindByEmailAsync(email);
            if (user == null) return; // Ne pas révéler que l'email n'existe pas

            var token = await _um.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);

            var resetLink = $"{_cfg["FrontendUrl"]}/auth/reset-password?email={email}&token={encodedToken}";

            var emailService = _ctx.HttpContext!.RequestServices.GetRequiredService<IEmailService>();
            await emailService.SendPasswordResetEmailAsync(email, resetLink);
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _um.FindByEmailAsync(email);
            if (user == null)
            {
                throw new ValidationException("Utilisateur non trouvé");
            }

            var result = await _um.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // Réinitialiser le statut de première connexion si nécessaire
            if (user.IsFirstLogin)
            {
                user.IsFirstLogin = false;
                await _um.UpdateAsync(user);
            }
        }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            var user = await _um.GetUserAsync(_ctx.HttpContext!.User);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non connecté");
            }

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                ProfilePicture = user.ProfilePicture,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Fonction = user.Fonction,
                Department = user.Department,
                Sector = user.Sector
            };
        }

        // ---- Helpers ----

        private string GenerateAccessToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email!),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("IsActive", user.IsActive.ToString()),
        new Claim("IsFirstLogin", user.IsFirstLogin.ToString()),
        new Claim("fullName", user.FullName ?? ""),
        new Claim("profilePicture", user.ProfilePicture ?? ""),
        new Claim("fonction", user.Fonction ?? ""),
        new Claim("sector", user.Sector ?? ""),
        new Claim("phoneNumber", user.PhoneNumber ?? ""),
        new Claim("department", user.Department.ToString())
    };

            if (user.SessionId != null)
            {
                claims.Add(new Claim("SessionId", user.SessionId.ToString()!));
            }

            // Ajouter les rôles dans le format attendu
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                roles.FirstOrDefault() ?? ""));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["JwtSettings:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _cfg["JwtSettings:Issuer"],
                audience: _cfg["JwtSettings:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private async Task UpdateRefreshToken(User user, string token, TimeSpan expiry)
        {
            user.RefreshToken = token;
            user.RefreshTokenExpiry = DateTime.UtcNow.Add(expiry);
            await _um.UpdateAsync(user);
        }
    }
}