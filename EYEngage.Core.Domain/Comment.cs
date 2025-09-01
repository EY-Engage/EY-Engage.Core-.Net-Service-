
namespace EYEngage.Core.Domain;

public class Comment
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; } = null!;
    public virtual Guid? AuthorId { get; set; }
    public  User Author { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CommentReaction> Reactions { get; set; } = new List<CommentReaction>();
    public ICollection<CommentReply> Replies { get; set; } = new List<CommentReply>();

}