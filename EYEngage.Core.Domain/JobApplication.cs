using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace EYEngage.Core.Domain;

public class JobApplication
{
    [Key] // Ajouter l'attribut [Key]
    public Guid Id { get; set; } = Guid.NewGuid(); // Propriété manquante
    public Guid JobOfferId { get; set; }
    public virtual JobOffer JobOffer { get; set; } = null!;

    public Guid? UserId { get; set; } // Null for external candidates
    public virtual User? User { get; set; }

    [MaxLength(100)]
    public string? CandidateName { get; set; }

    [MaxLength(100)]
    public string? CandidateEmail { get; set; }

    [MaxLength(20)]
    public string? CandidatePhone { get; set; }

    public string? CoverLetter { get; set; }
    public string? ResumeFilePath { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public bool IsRecommended { get; set; }
    public bool IsPreSelected { get; set; } // IA selection

    public Guid? RecommendedByUserId { get; set; }
    public virtual User? RecommendedBy { get; set; }
    public double Score { get; set; }
    public string? Justification { get; set; }

}

public enum ApplicationStatus { Pending, Reviewed, Selected, Rejected }
