// Core/Application/Dto/EventDto/ParticipationRequestDto.cs
namespace EYEngage.Core.Application.Dto.EventDto;

public record ParticipationRequestDto(
    Guid ParticipationId,
    Guid? UserId,
    string FullName,
    string Email,
    string? ProfilePicture,
    DateTime RequestedAt
);
