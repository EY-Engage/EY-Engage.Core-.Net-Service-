using EYEngage.Core.Application.Dto.EventDto;
using EYEngage.Core.Application.Dto;
using EYEngage.Core.Domain;
using static EYEngage.Core.Application.Services.EventService;


namespace EYEngage.Core.Application.InterfacesServices;

public interface IEventService
{
    Task<EventDto> CreateEventAsync(Guid organizerId, CreateEventDto dto);
    Task<IEnumerable<EventDto>> GetEventsByStatusAsync(EventStatus status, Guid userId, string? department = null);
    Task RequestParticipationAsync(Guid eventId, Guid userId);
    Task ApproveParticipationAsync(Guid participationId, Guid approvedById);
    Task<IReadOnlyList<UserDto>> GetInterestedUsersAsync(Guid eventId);
    Task CommentAsync(Guid eventId, Guid authorId, string content);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid eventId);
    Task UpdateEventStatusAsync(Guid eventId, EventStatus status, Guid approvedById);
    Task<IReadOnlyList<ParticipationRequestDto>> GetParticipationRequestsAsync(Guid eventId);
    Task ToggleInterestAsync(Guid eventId, Guid userId);
    Task<IReadOnlyList<UserDto>> GetParticipantsAsync(Guid eventId);
    Task<bool> IsUserInterested(Guid eventId, Guid userId);
    Task<EventParticipation> GetUserParticipationStatus(Guid eventId, Guid userId);
    Task RejectParticipationAsync(Guid participationId);
    Task ReactToCommentAsync(Guid commentId, Guid userId, string emoji);
    Task<IReadOnlyList<CommentReactionDto>> GetReactionsForCommentAsync(Guid commentId);
    Task ReplyToCommentAsync(Guid commentId, Guid authorId, string content);
    Task<IReadOnlyList<CommentReplyDto>> GetRepliesForCommentAsync(Guid commentId);
    Task DeleteCommentAsync(Guid commentId, Guid userId);
    Task ReactToReplyAsync(Guid replyId, Guid userId, string emoji);
    Task DeleteReplyAsync(Guid replyId, Guid userId);
    Task<IEnumerable<object>> GetReplyReactionsAsync(Guid replyId);
    Task<UserProfileEventsDto> GetUserProfileEventsAsync(Guid userId);
    Task DeleteEventAsync(Guid eventId, Guid requesterId);
    Task<EventAnalyticsDto> GetEventAnalyticsAsync(Guid? userId = null, string? department = null);
    Task<IEnumerable<EventTrendDto>> GetEventTrendsAsync(Guid? userId = null, string? department = null);

}
