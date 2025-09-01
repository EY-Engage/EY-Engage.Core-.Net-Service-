

using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Dto.UserDtos;


    public record UserPublicProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Fonction { get; set; }
        public Department Department { get; set; } 
        public string? Sector { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Informations publiques supplémentaires
        public List<string>? Roles { get; set; }
        public DateTime? LastLoginAt { get; set; }

}
