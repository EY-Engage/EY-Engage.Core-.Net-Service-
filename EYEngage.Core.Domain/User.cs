using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EYEngage.Core.Domain;

public enum Department { Assurance, Consulting, StrategyAndTransactions, Tax }

public class User : IdentityUser<Guid>
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(500)]
    public string? ProfilePicture { get; set; }

    [Required, MaxLength(100)]
    public string Fonction { get; set; } = null!;

    public Department Department { get; set; }

    [MaxLength(100)]
    public string Sector { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = false;
    public bool IsFirstLogin { get; set; } = true;
    public Guid? SessionId { get; set; }

    // Navigation properties
    public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventParticipation> EventParticipations { get; set; } = new List<EventParticipation>();
    public virtual ICollection<EventInterest> EventInterests { get; set; } = new List<EventInterest>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<CommentReply> CommentReplies { get; set; } = new List<CommentReply>();
    public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();
    public virtual ICollection<CommentReplyReaction> CommentReplyReactions { get; set; } = new List<CommentReplyReaction>();
}