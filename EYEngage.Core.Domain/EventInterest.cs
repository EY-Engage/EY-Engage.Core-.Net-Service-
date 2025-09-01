

namespace EYEngage.Core.Domain;

public class EventInterest
{
    public Guid? EventId { get; set; }
    public virtual Event? Event { get; set; } = null!;
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; } = null!;
    public DateTime InterestedAt { get; set; } = DateTime.UtcNow;
}
