using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Application.Dto.AuthDtos;

public record ValidateResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!; public string Email { get; init; } = null!;

    public bool IsFirstLogin { get; init; }
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = new();
    public Guid? SessionId { get; set; }
}
