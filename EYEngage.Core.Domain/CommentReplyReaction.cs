

namespace EYEngage.Core.Domain;

public class CommentReplyReaction
{
    public Guid Id { get; set; }
    public Guid? ReplyId { get; set; }
    public virtual CommentReply? Reply { get; set; }
    public Guid? UserId { get; set; }
    public virtual  User? User { get; set; } 
    public string Emoji { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
