

namespace EYEngage.Core.Application.Dto.EventDto;

public record MonthlyStatsDto(
       int Year,
       int Month,
       int EventsCount,
       int ParticipantsCount,
       int InterestsCount
   );
