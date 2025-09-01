

namespace EYEngage.Core.Application.Dto.EventDto;

public record CommentReactionDto(
    Guid Id,
    Guid? CommentId,
    Guid? UserId,
    string UserFullName,
    string? UserProfilePicture,
    string Emoji,
    DateTime CreatedAt
);

