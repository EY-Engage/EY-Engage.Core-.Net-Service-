

namespace EYEngage.Core.Domain;

public class CommentReply
{
    public Guid Id { get; set; }
    public Guid? CommentId { get; set; }
    public virtual Comment? Comment { get; set; } 
    public Guid? AuthorId { get; set; }
    public  virtual User? Author { get; set; } 
    public string Content { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CommentReplyReaction> Reactions { get; set; } = new List<CommentReplyReaction>();

}
