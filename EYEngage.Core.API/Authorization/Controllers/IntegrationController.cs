using Microsoft.AspNetCore.Mvc;
using EYEngage.Core.Domain;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EYEngage.Core.API.Controllers;

[Route("api/integration")]
[ApiController]
public class IntegrationController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly IJobService _jobService;
    private readonly ILogger<IntegrationController> _logger;
    private readonly UserManager<User> _userManager;

    public IntegrationController(
        IUserService userService,
        IEventService eventService,
        IJobService jobService,
        ILogger<IntegrationController> logger, UserManager<User> _um)
    {
        _userService = userService;
        _eventService = eventService;
        _jobService = jobService;
        _logger = logger;
        _userManager= _um;
    }

    // ENDPOINTS POUR NESTJS

    [HttpGet("events/{id}")]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        try
        {
            // Logique pour récupérer un événement par ID
            // À implémenter selon votre EventService
            return Ok(new { id, message = "Event data" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event {EventId}", id);
            return NotFound();
        }
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return NotFound();
        }
    }

    [HttpGet("users/by-department/{department}")]
    public async Task<IActionResult> GetUsersByDepartment(Department department)
    {
        try
        {
            // Logique pour récupérer les utilisateurs par département
            var users = await _userService.GetAllUsersAsync();
            var filteredUsers = users.Where(u => u.Department == department).ToList();
            return Ok(filteredUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for department {Department}", department);
            return BadRequest();
        }
    }

    [HttpGet("users/filtered")]
    public async Task<IActionResult> GetFilteredUsers(
        [FromQuery] Department? department,
        [FromQuery] string? roles)
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            var filtered = users.AsQueryable();

            if (department.HasValue)
            {
                filtered = filtered.Where(u => u.Department == department);
            }

            // Filtrage par rôles si nécessaire
            // À implémenter selon vos besoins

            return Ok(filtered.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered users");
            return BadRequest();
        }
    }

    [HttpGet("jobs/{id}")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        try
        {
            var job = await _jobService.GetJobOfferByIdAsync(id);
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {JobId}", id);
            return NotFound();
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetIntegrationStats()
    {
        try
        {
            // Statistiques d'intégration
            return Ok(new
            {
                totalUsers = await GetTotalUsersCount(),
                totalEvents = await GetTotalEventsCount(),
                totalJobs = await GetTotalJobsCount(),
                lastSync = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting integration stats");
            return StatusCode(500);
        }
    }

    // WEBHOOKS DEPUIS NESTJS

    [HttpPost("social-activity")]
    public async Task<IActionResult> HandleSocialActivity([FromBody] SocialActivityDto activity)
    {
        try
        {
            _logger.LogInformation("Received social activity: {ActivityType} from user {UserId}",
                activity.ActivityType, activity.UserId);

            // Traiter l'activité sociale
            // Par exemple, mettre à jour des métriques, logs, etc.

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling social activity");
            return BadRequest();
        }
    }

    [HttpPost("chat-activity")]
    public async Task<IActionResult> HandleChatActivity([FromBody] ChatActivityDto activity)
    {
        try
        {
            _logger.LogInformation("Received chat activity: {ActivityType} from user {UserId} in conversation {ConversationId}",
                activity.ActivityType, activity.UserId, activity.ConversationId);

            // Traiter l'activité de chat
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling chat activity");
            return BadRequest();
        }
    }
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserForSocial(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest("Invalid user ID format");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var userData = new
            {
                id = user.Id.ToString(),
                fullName = user.FullName,
                email = user.Email,
                profilePicture = user.ProfilePicture,
                phoneNumber = user.PhoneNumber,
                fonction = user.Fonction,
                department = user.Department.ToString(),
                sector = user.Sector,
                isActive = user.IsActive,
                isFirstLogin = user.IsFirstLogin,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt,
                roles = roles.ToArray()
            };

            return Ok(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving user data for NestJS: {userId}");
            return StatusCode(500, "Internal server error");
        }
    }

    // Endpoint pour récupérer plusieurs utilisateurs
    [HttpPost("users/batch")]
    public async Task<IActionResult> GetUsersForSocial([FromBody] string[] userIds)
    {
        try
        {
            var users = new List<object>();

            foreach (var userId in userIds)
            {
                if (Guid.TryParse(userId, out var userGuid))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        users.Add(new
                        {
                            id = user.Id.ToString(),
                            fullName = user.FullName,
                            email = user.Email,
                            profilePicture = user.ProfilePicture,
                            phoneNumber = user.PhoneNumber,
                            fonction = user.Fonction,
                            department = user.Department.ToString(),
                            sector = user.Sector,
                            isActive = user.IsActive,
                            isFirstLogin = user.IsFirstLogin,
                            createdAt = user.CreatedAt,
                            updatedAt = user.UpdatedAt,
                            roles = roles.ToArray()
                        });
                    }
                }
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch user data for NestJS");
            return StatusCode(500, "Internal server error");
        }
    }

    // Endpoint pour que NestJS récupère des suggestions de follow basées sur le département
    [HttpGet("user/{userId}/follow-suggestions")]
    public async Task<IActionResult> GetFollowSuggestions(string userId, [FromQuery] int limit = 10)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest("Invalid user ID format");
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                return NotFound("User not found");
            }

            // Récupérer les utilisateurs du même département (exclure l'utilisateur actuel)
            var suggestions = await _userManager.Users
                .Where(u => u.Department == currentUser.Department && u.Id != userGuid && u.IsActive)
                .OrderBy(u => Guid.NewGuid()) // Random order
                .Take(limit)
                .Select(u => new
                {
                    id = u.Id.ToString(),
                    fullName = u.FullName,
                    email = u.Email,
                    profilePicture = u.ProfilePicture,
                    fonction = u.Fonction,
                    department = u.Department.ToString(),
                    sector = u.Sector
                })
                .ToListAsync();

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving follow suggestions for user: {userId}");
            return StatusCode(500, "Internal server error");
        }
    }

    // Endpoint pour valider un token JWT (utilisé par NestJS)
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            // Ici vous pouvez valider le token et retourner les données utilisateur
            // Cette logique dépend de votre implémentation JWT

            return Ok(new { valid = true, message = "Token is valid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return BadRequest(new { valid = false, message = "Invalid token" });
        }
    }

    // Endpoint pour récupérer les statistiques utilisateur pour le dashboard admin
    [HttpGet("users/stats")]
    [Authorize(Roles = "SuperAdmin,Admin,AgentEY")]
    public async Task<IActionResult> GetUserStats([FromQuery] string? department = null)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            // Filtrer par département si spécifié
            if (!string.IsNullOrEmpty(department) && Enum.TryParse<Department>(department, out var dept))
            {
                query = query.Where(u => u.Department == dept);
            }

            var stats = new
            {
                totalUsers = await query.CountAsync(),
                activeUsers = await query.CountAsync(u => u.IsActive),
                inactiveUsers = await query.CountAsync(u => !u.IsActive),
                newUsersThisMonth = await query.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1)),
                departmentBreakdown = await query
                    .GroupBy(u => u.Department)
                    .Select(g => new { department = g.Key.ToString(), count = g.Count() })
                    .ToListAsync()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user stats");
            return StatusCode(500, "Internal server error");
        }
    }


    // MÉTHODES PRIVÉES

    private async Task<int> GetTotalUsersCount()
    {
        var users = await _userService.GetAllUsersAsync();
        return users.Count();
    }

    private async Task<int> GetTotalEventsCount()
    {
        // À implémenter selon votre EventService
        return 0;
    }

    private async Task<int> GetTotalJobsCount()
    {
        var jobs = await _jobService.GetJobOffersAsync();
        return jobs.Count();
    }
    public class SocialActivityDto
    {
        public string UserId { get; set; }
        public string ActivityType { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }

    public class ChatActivityDto
    {
        public string UserId { get; set; }
        public string ConversationId { get; set; }
        public string ActivityType { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }
    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}