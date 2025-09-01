using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Application.Dto.AuthDtos;

public record RefreshRequestDto
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

