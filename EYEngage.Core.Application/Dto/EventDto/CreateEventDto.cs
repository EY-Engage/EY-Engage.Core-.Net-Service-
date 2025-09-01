using Microsoft.AspNetCore.Http;
using System;


namespace EYEngage.Core.Application.Dto.EventDto;

public record CreateEventDto(
 string Title,
 string Description,
 DateTime Date,
 string Location,
 IFormFile? ImageFile
);
