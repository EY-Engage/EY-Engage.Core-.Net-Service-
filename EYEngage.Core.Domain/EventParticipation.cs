using EYEngage.Core.Domain;

public enum ParticipationStatus {Pending, Approved, Rejected }

public class EventParticipation
{
    public Guid Id { get; set; }
    public Guid? EventId { get; set; }
    public virtual Event? Event { get; set; } = null!;
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; } = null!;
    public Guid? ApprovedById { get; set; }
    public virtual User? ApprovedBy { get; set; }
    public ParticipationStatus Status { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
