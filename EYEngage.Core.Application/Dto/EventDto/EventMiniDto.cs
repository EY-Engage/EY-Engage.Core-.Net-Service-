// EventMiniDto.cs
namespace EYEngage.Core.Application.Dto.EventDto;

public record EventMiniDto(
    Guid Id,
    string Title,
    DateTime Date,
    string Location,
    string? ImagePath
);