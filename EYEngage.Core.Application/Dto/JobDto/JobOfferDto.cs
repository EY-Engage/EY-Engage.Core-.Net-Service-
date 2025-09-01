using EYEngage.Core.Domain;
using System.ComponentModel.DataAnnotations;


namespace EYEngage.Core.Application.Dto.JobDto;

public record JobOfferDto
{
    public Guid Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Description { get; set; } = null!;
    [Required]
    public string KeySkills { get; set; } = null!;
    [Required]
    public string ExperienceLevel { get; set; } = null!;
    [Required]
    public string Location { get; set; } = null!;
    public DateTime PublishDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsInternalOnly { get; set; }
    public Guid PublisherId { get; set; }
    public string? PublisherFullName { get; set; }
    public Department Department { get; set; }
    public JobType JobType { get; set; }
    public int ApplicationsCount { get; set; }
}
