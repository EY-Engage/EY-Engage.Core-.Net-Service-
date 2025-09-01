


namespace EYEngage.Core.Application.Dto.EventDto;

public record EventAnalyticsDto(
    int TotalEvents,
    int TotalParticipants,
    int TotalInterests,
    double AvgParticipationPerEvent,
    double ParticipationRate,
    List<PopularEventDto> PopularEvents,
    List<DepartmentStatsDto> DepartmentStats,
    List<MonthlyStatsDto> MonthlyStats
);
