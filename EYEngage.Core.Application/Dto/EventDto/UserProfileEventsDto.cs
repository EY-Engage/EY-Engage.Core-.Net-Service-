// UserProfileEventsDto.cs
using System.Collections.Generic;

namespace EYEngage.Core.Application.Dto.EventDto;

public record UserProfileEventsDto
{
    public List<EventMiniDto> OrganizedEvents { get; init; }
    public List<EventMiniDto> ParticipatedEvents { get; init; }
    public List<EventMiniDto> ApprovedEvents { get; init; }
    public List<ApprovedParticipationDto> ApprovedParticipations { get; init; }
    public List<CommentDto> Comments { get; init; }
}