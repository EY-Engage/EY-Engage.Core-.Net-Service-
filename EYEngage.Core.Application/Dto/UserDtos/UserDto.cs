using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Dto;

public record UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ProfilePicture { get; set; }

    public string PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Fonction { get; set; }
    public Department Department { get; set; }
    public string[] Roles { get; set; }
    public string Sector { get; set; }
    public bool IsActive { get; set; }
}
