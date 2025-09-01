
namespace EYEngage.Core.Domain;

public enum EventStatus { Draft, Pending, Approved, Rejected }

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Location { get; set; } = null!;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public string? ImagePath { get; set; }

    public Guid? OrganizerId { get; set; }
    public virtual User? Organizer { get; set; }
    public Guid? ApprovedById { get; set; }
    public virtual User? ApprovedBy { get; set; }

    public virtual ICollection<EventParticipation> Participations { get; set; } = new List<EventParticipation>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<EventInterest> Interests { get; set; } = new List<EventInterest>();
}