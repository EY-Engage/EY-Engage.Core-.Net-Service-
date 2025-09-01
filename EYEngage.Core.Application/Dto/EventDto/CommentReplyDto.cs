

namespace EYEngage.Core.Application.Dto.EventDto;

public record CommentReplyDto(
 Guid Id,
 Guid? CommentId,
 Guid? AuthorId,
 string AuthorFullName,
 string? AuthorProfilePicture,
 string Content,
 DateTime CreatedAt
);
