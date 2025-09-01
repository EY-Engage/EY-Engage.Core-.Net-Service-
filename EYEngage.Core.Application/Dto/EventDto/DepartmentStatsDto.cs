

namespace EYEngage.Core.Application.Dto.EventDto;

public record DepartmentStatsDto(
    string DepartmentId,   // Changer Guid en string
    string DepartmentName,
    int TotalEvents,
    int TotalParticipants,
    int TotalInterests
);
