using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EYEngage.Core.API.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _excludedPaths;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
            _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/refresh",
                "/api/user/check-email",
                "/swagger",
                "/health",
                "/.well-known"
            };
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Ignorer les routes exclues
            if (path != null && _excludedPaths.Any(excluded => path.StartsWith(excluded)))
            {
                await _next(context);
                return;
            }

            // Ignorer les fichiers statiques
            if (path != null && (path.Contains(".") && !path.Contains("/api/")))
            {
                await _next(context);
                return;
            }

            // Vérifier si l'utilisateur est authentifié
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var sessionIdClaim = context.User.FindFirst("SessionId");
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

                    if (sessionIdClaim != null && userIdClaim != null)
                    {
                        var user = await userManager.FindByIdAsync(userIdClaim.Value);

                        // Vérifier si l'utilisateur existe et si la session est valide
                        if (user == null || user.SessionId?.ToString() != sessionIdClaim.Value)
                        {
                            await WriteUnauthorizedResponse(context, "Session invalidée");
                            return;
                        }

                        // Vérifier si l'utilisateur est toujours actif (sauf SuperAdmin)
                        var roles = context.User.Claims
                            .Where(c => c.Type == ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();

                        if (!roles.Contains("SuperAdmin") && !user.IsActive)
                        {
                            await WriteUnauthorizedResponse(context, "Compte désactivé");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log l'erreur (utilisez votre système de logging)
                    Console.WriteLine($"Erreur dans SessionValidationMiddleware: {ex.Message}");
                    await WriteUnauthorizedResponse(context, "Erreur de validation de session");
                    return;
                }
            }

            await _next(context);
        }

        private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"error\":\"{message}\"}}");
        }
    }
}