using EYEngage.Core.Application.Dto.EventDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EYEngage.Core.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class EventController(IEventService _svc, UserManager<User> _um, GeminiService _gm) : BaseController
    {


        private Guid GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
                throw new UnauthorizedAccessException("Invalid or missing user ID.");

            return guid;
        }


        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> Create([FromForm] CreateEventDto dto)
        {
            var userId = GetCurrentUserId();
            var ev = await _svc.CreateEventAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
        }

        // Accessible à tous les rôles authentifiés
        [HttpGet("status/{status}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetByStatus(EventStatus status, [FromQuery] string? department = null)
        {
            var currentUserId = GetCurrentUserId();
            var events = await _svc.GetEventsByStatusAsync(status, currentUserId, department);
            return Ok(events);
        }

        // Accessible à tous les rôles authentifiés
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var events = await _svc.GetEventsByStatusAsync(EventStatus.Approved, currentUserId);
            var ev = events.SingleOrDefault(x => x.Id == id);
            return ev == null ? NotFound() : Ok(ev);
        }

        // Accessible à tous les rôles authentifiés
        [HttpPost("{id:guid}/participate")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> Participate(Guid id)
        {
            var userId = GetCurrentUserId();
            await _svc.RequestParticipationAsync(id, userId);
            return NoContent();
        }

        // Admin et AgentEY seulement
        [HttpPost("{id:guid}/approveEvent")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> ApproveEvent(Guid id)
        {
            var userId = GetCurrentUserId();
            await _svc.UpdateEventStatusAsync(id, EventStatus.Approved, userId);
            return NoContent();
        }

        // Admin et AgentEY seulement
        [HttpPost("participation/{pid:guid}/approve")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> Approve(Guid pid)
        {
            var userId = GetCurrentUserId();
            await _svc.ApproveParticipationAsync(pid, userId);
            return NoContent();
        }

        // Accessible à tous
        [HttpPost("{id:guid}/toggleInterest")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> ToggleInterest(Guid id)
        {
            var userId = GetCurrentUserId();
            await _svc.ToggleInterestAsync(id, userId);
            return NoContent();
        }

        // Accessible à tous
        [HttpGet("{id:guid}/interestedUsers")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetInterestedUsers(Guid id)
            => Ok(await _svc.GetInterestedUsersAsync(id));

        // Accessible à tous
        [HttpGet("{id:guid}/comments")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> Comments(Guid id)
            => Ok(await _svc.GetCommentsAsync(id));

        // Accessible à tous
        [HttpPost("{id:guid}/comment")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> Comment(Guid id, [FromBody] string content)
        {
            var userId = GetCurrentUserId();
            await _svc.CommentAsync(id, userId, content);
            return NoContent();
        }

        // Admin et AgentEY seulement
        [HttpPost("{id:guid}/rejectEvent")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> RejectEvent(Guid id)
        {
            var userId = GetCurrentUserId();
            await _svc.UpdateEventStatusAsync(id, EventStatus.Rejected, userId);
            return NoContent();
        }

        // Admin et AgentEY seulement
        [HttpGet("{id:guid}/requests")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> GetRequests(Guid id)
            => Ok(await _svc.GetParticipationRequestsAsync(id));

        // Accessible à tous
        [HttpGet("{eventId}/participants")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetParticipants(Guid eventId)
        {
            var users = await _svc.GetParticipantsAsync(eventId);
            return Ok(users);
        }

        // Accessible à tous
        [HttpGet("{eventId}/user-status")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetUserEventStatus(Guid eventId)
        {
            var userId = GetCurrentUserId();
            var isInterested = await _svc.IsUserInterested(eventId, userId);
            var participation = await _svc.GetUserParticipationStatus(eventId, userId);

            return Ok(new
            {
                isInterested,
                participationStatus = participation?.Status.ToString()
            });
        }

        // Admin et AgentEY seulement
        [HttpPost("participation/{participationId}/reject")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> RejectParticipation(Guid participationId)
        {
            await _svc.RejectParticipationAsync(participationId);
            return Ok();
        }

        // Accessible à tous
        [HttpPost("comments/{commentId}/react")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> ReactToComment(Guid commentId, [FromBody] string emoji)
        {
            var userId = GetCurrentUserId();
            await _svc.ReactToCommentAsync(commentId, userId, emoji);
            return NoContent();
        }

        // Accessible à tous
        [HttpGet("comments/{commentId}/reactions")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetReactions(Guid commentId)
            => Ok(await _svc.GetReactionsForCommentAsync(commentId));

        // Accessible à tous
        [HttpPost("comments/{commentId}/reply")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> ReplyToComment(Guid commentId, [FromBody] string content)
        {
            var userId = GetCurrentUserId();
            await _svc.ReplyToCommentAsync(commentId, userId, content);
            return NoContent();
        }

        // Accessible à tous
        [HttpGet("comments/{commentId}/replies")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetReplies(Guid commentId)
            => Ok(await _svc.GetRepliesForCommentAsync(commentId));

        // Accessible à tous
        [HttpDelete("comments/{commentId}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            await _svc.DeleteCommentAsync(commentId, userId);
            return NoContent();
        }

        // Accessible à tous
        [HttpPost("replies/{replyId}/react")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> ReactToReply(Guid replyId, [FromBody] string emoji)
        {
            var userId = GetCurrentUserId();
            await _svc.ReactToReplyAsync(replyId, userId, emoji);
            return NoContent();
        }

        // Accessible à tous
        [HttpDelete("replies/{replyId}")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> DeleteReply(Guid replyId)
        {
            var userId = GetCurrentUserId();
            await _svc.DeleteReplyAsync(replyId, userId);
            return NoContent();
        }

        // Accessible à tous
        [HttpGet("replies/{replyId}/reactions")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetReplyReactions(Guid replyId)
        {
            var result = await _svc.GetReplyReactionsAsync(replyId);
            return Ok(result);
        }

        // Ajout dans EventController.cs

        [HttpGet("profile-events")]
        [Authorize(Roles = "SuperAdmin,Admin,AgentEY,EmployeeEY")]
        public async Task<IActionResult> GetProfileEvents([FromQuery] Guid? userId = null)
        {
            // Si aucun userId spécifié, utiliser l'utilisateur connecté
            Guid targetUserId = userId ?? GetCurrentUserId();

            // Vérifier que l'utilisateur a le droit d'accéder à ces données
            var currentUserId = GetCurrentUserId();
            var isOwnProfile = targetUserId == currentUserId;

            // Pour l'instant, tout utilisateur authentifié peut voir les événements publics d'un autre
            // Vous pouvez ajouter des restrictions ici si nécessaire

            var events = await _svc.GetUserProfileEventsAsync(targetUserId);
            return Ok(events);
        }

        // Admin et AgentEY seulement
        [HttpDelete("{eventId}")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> DeleteEvent(Guid eventId)
        {
            var userId = GetCurrentUserId();
            await _svc.DeleteEventAsync(eventId, userId);
            return NoContent();
        }

        // Admin et AgentEY seulement - Analytics
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> GetEventAnalytics([FromQuery] string? department = null)
        {
            Guid? userId = null;

            if (User.IsInRole("AgentEY"))
            {
                var user = await _um.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var analytics = await _svc.GetEventAnalyticsAsync(userId, department);
            return Ok(analytics);
        }

        // Admin et AgentEY seulement - Trends
        [HttpGet("trends")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> GetEventTrends([FromQuery] string? department = null)
        {
            Guid? userId = null;

            if (User.IsInRole("AgentEY"))
            {
                var user = await _um.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var trends = await _svc.GetEventTrendsAsync(userId, department);
            return Ok(trends);
        }

        // Admin et AgentEY seulement - Report
        [HttpPost("analytics/report")]
        [Authorize(Roles = "Admin,AgentEY")]
        public async Task<IActionResult> GenerateAnalyticsReport([FromQuery] string? department = null)
        {
            var userId = User.IsInRole("AgentEY") ? GetCurrentUserId() : (Guid?)null;
            var analytics = await _svc.GetEventAnalyticsAsync(userId, department);
            var report = await _gm.GenerateReport(analytics);
            return Ok(new { report });
        }
    }
}