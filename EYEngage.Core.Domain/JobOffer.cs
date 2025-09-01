using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EYEngage.Core.Domain;

// JobOffer.cs
public class JobOffer 
{
    [Key] // Ajouter l'attribut [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required, MaxLength(500)]
    public string KeySkills { get; set; } = null!;

    [Required]
    public string ExperienceLevel { get; set; } = null!; // "Junior", "Mid", "Senior"

    [Required, MaxLength(100)]
    public string Location { get; set; } = null!;

    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public DateTime? CloseDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsInternalOnly { get; set; } = false;

    public Guid PublisherId { get; set; }
    public virtual User Publisher { get; set; } = null!;

    public Department Department { get; set; }
    public JobType JobType { get; set; } // Enum: FullTime, PartTime, Contract, Internship

    public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}

public enum JobType { FullTime, PartTime, Contract, Internship }

