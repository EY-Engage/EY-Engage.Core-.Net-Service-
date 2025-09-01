using EYEngage.Core.Domain;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime Date,
    string Location,
    string? ImagePath,
    EventStatus Status,
    int InterestedCount,
    int ParticipantCount,
    bool IsCurrentUserInterested,
    string? CurrentUserParticipationStatus,
    string OrganizerName,
    string OrganizerDepartement// Nouvelle propriété ajoutée
);