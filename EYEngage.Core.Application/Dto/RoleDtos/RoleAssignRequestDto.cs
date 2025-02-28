using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Application.Dto.RoleDtos
{
    public class RoleAssignRequestDto
    {
        public Guid UserId { get; set; }
        public string RoleName { get; set; } = null!;
    }
}
