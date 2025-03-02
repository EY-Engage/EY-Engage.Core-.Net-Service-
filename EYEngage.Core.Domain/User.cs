using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Domain;

public class User : IdentityUser<Guid>
{
    [Required, MaxLength(100)]
    public string FullName { get; set; }

    [MaxLength(500)]
    public string? ProfilePicture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}