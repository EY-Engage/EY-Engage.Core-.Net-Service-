namespace EYEngage.Core.Application.Dto;
public record ApprovedParticipationDto(
    Guid ParticipationId,
    Guid? EventId,
    string EventTitle,
    string ParticipantFullName,
    string? ParticipantProfilePicture
);
