

namespace EYEngage.Core.Application.Dto.EventDto;

public record EventTrendDto(
   Guid EventId,
   string Title,
   DateTime Date,
   int Participants,
   int Interests,
   double ConversionRate
);
