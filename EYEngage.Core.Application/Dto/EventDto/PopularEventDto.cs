

namespace EYEngage.Core.Application.Dto.EventDto;

public record PopularEventDto(
        Guid EventId,
        string Title,
        DateTime Date,
        int Participants,
        int Interests
    );
