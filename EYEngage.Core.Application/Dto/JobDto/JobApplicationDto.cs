using EYEngage.Core.Domain;

namespace EYEngage.Core.Application.Dto.JobDto;

public record JobApplicationDto
{
    public Guid Id { get; set; }
    public Guid JobOfferId { get; set; }
    public Guid? UserId { get; set; }
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CandidatePhone { get; set; }
    public string? CoverLetter { get; set; }
    public string? ResumeFilePath { get; set; }
    public DateTime? AppliedAt { get; set; }
    public ApplicationStatus? Status { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsPreSelected { get; set; }
    public Guid? RecommendedByUserId { get; set; }
    public string? RecommendedByFullName { get; set; }
    public double? Score { get; set; }
    public string? Justification { get; set; }
}
