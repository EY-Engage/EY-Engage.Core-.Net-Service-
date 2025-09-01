using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.AuthDtos;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace EYEngage.Core.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService authService)
    {
        _auth = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var result = await _auth.LoginAsync(dto);

            // Si l'utilisateur doit changer son mot de passe
            if (!result.IsActive || result.IsFirstLogin)
            {
                return Ok(new
                {
                    needsPasswordChange = true,
                    email = result.Email,
                    roles = result.Roles,
                    isActive = result.IsActive,
                    isFirstLogin = result.IsFirstLogin,
                    fullName = result.FullName,
                    id = result.Id
                });
            }

            // Utilisateur normal - définir les cookies
            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(new
            {
                success = true,
                email = result.Email,
                roles = result.Roles,
                isActive = result.IsActive,
                isFirstLogin = result.IsFirstLogin,
                fullName = result.FullName,
                id = result.Id,
                user = new
                {
                    id = result.Id,
                    email = result.Email,
                    fullName = result.FullName
                }
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var result = await _auth.ChangePasswordAsync(dto);

            // Définir les cookies après changement réussi
            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(new
            {
                success = true,
                email = dto.Email,
                roles = result.Roles,
                isActive = true,
                isFirstLogin = false,
                fullName = result.FullName,
                id = result.Id,
                user = new
                {
                    id = result.Id,
                    email = dto.Email,
                    fullName = result.FullName
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenResponseDto dto = null)
    {
        try
        {
            // Récupération du token depuis les cookies ou le body
            var refreshToken = Request.Cookies["ey-refresh"];

            if (string.IsNullOrEmpty(refreshToken) && dto != null)
            {
                refreshToken = dto.RefreshToken;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token manquant" });
            }

            var result = await _auth.RefreshTokenAsync(refreshToken);

            // Mettre à jour les cookies
            SetAuthCookies(result.AccessToken, result.RefreshToken);

            return Ok(new
            {
                success = true,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            });
        }
        catch (SecurityTokenException ex)
        {
            // Token invalide ou expiré
            ClearAuthCookies();
            return Unauthorized(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            ClearAuthCookies();
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erreur lors du rafraîchissement du token" });
        }
    }

    [Authorize]
    [HttpGet("validate")]
    public async Task<IActionResult> Validate()
    {
        try
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { error = "Not authenticated" });
            }

            var result = await _auth.ValidateAsync();

            return Ok(new
            {
                email = result.Email,
                roles = result.Roles,
                isActive = result.IsActive,
                isFirstLogin = result.IsFirstLogin,
                fullName = result.FullName,
                user = new
                {
                    id = result.Id.ToString(),
                    email = result.Email,
                    fullName = result.FullName
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Session invalid" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in validate endpoint: {ex.Message}");
            return Unauthorized(new { error = "Session validation failed" });
        }
    }

    [Authorize]
    [HttpGet("current-user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var user = await _auth.GetCurrentUserAsync();
            return Ok(user);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Appel du service logout seulement si l'utilisateur est authentifié
            if (User.Identity?.IsAuthenticated == true)
            {
                await _auth.LogoutAsync();
            }

            ClearAuthCookies();
            return Ok(new { message = "Déconnexion réussie" });
        }
        catch (Exception)
        {
            // En cas d'erreur, on clear les cookies quand même
            ClearAuthCookies();
            return Ok(new { message = "Déconnexion effectuée" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        await _auth.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "Email de réinitialisation envoyé si l'email existe" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        await _auth.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
        return Ok(new { message = "Mot de passe réinitialisé avec succès" });
    }

    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        var token = Guid.NewGuid().ToString();

        Response.Cookies.Append("csrf-token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // true en production
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(30)
        });

        return Ok(new { csrfToken = token });
    }

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Secure = false // true en production avec HTTPS
        };

        // Cookie de session - 15 minutes
        Response.Cookies.Append("ey-session", accessToken, new CookieOptions(cookieOptions)
        {
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        // Cookie de refresh - 7 jours
        Response.Cookies.Append("ey-refresh", refreshToken, new CookieOptions(cookieOptions)
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private void ClearAuthCookies()
    {
        var clearOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Path = "/",
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false // true en production
        };

        Response.Cookies.Append("ey-session", "", clearOptions);
        Response.Cookies.Append("ey-refresh", "", clearOptions);
        Response.Cookies.Append("csrf-token", "", clearOptions);
    }
}

