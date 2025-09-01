

namespace EYEngage.Core.Domain;

public class CommentReaction
{
    public Guid Id { get; set; }
    public Guid? CommentId { get; set; }
    public virtual Comment? Comment { get; set; } 
    public  Guid? UserId { get; set; }
    public  virtual User? User { get; set; } 
    public string Emoji { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
