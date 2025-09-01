using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;


namespace EYEngage.Infrastructure;

public class EYEngageDbContext : IdentityDbContext<User, Role, Guid>
{
    public DbSet<Event> Events { get; set; }
    public DbSet<EventParticipation> EventParticipations { get; set; }
    public DbSet<EventInterest> EventInterests { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentReaction> CommentReactions { get; set; }
    public DbSet<CommentReply> CommentReplies { get; set; }
    public DbSet<CommentReplyReaction> CommentReplyReactions { get; set; }
    public DbSet<JobOffer> JobOffers { get; set; } 
    public DbSet<JobApplication> JobApplications { get; set; } 

    public EYEngageDbContext(DbContextOptions<EYEngageDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventParticipation>()
            .HasIndex(p => new { p.EventId, p.UserId })
            .IsUnique();

        builder.Entity<EventInterest>()
            .HasKey(ei => new { ei.EventId, ei.UserId });

        builder.Entity<CommentReaction>()
            .HasKey(r => new { r.CommentId, r.UserId });

        builder.Entity<CommentReplyReaction>()
            .HasKey(r => new { r.ReplyId, r.UserId });

        // Event -> Organizer (User)
        builder.Entity<Event>()
       .HasOne(e => e.Organizer)
       .WithMany(u => u.OrganizedEvents)
       .HasForeignKey(e => e.OrganizerId)
       .OnDelete(DeleteBehavior.Cascade); // Changé de Restrict à Cascade // Ne pas supprimer l’organisateur si event supprimé

        // Event -> Comments
        builder.Entity<Comment>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Comments)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade); // Suppression cascade de commentaires si event supprimé

        // Comment -> Author (User)
        builder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> Reactions
        builder.Entity<CommentReaction>()
            .HasOne(r => r.Comment)
            .WithMany(c => c.Reactions)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade); // Supprimer réactions quand commentaire supprimé

        builder.Entity<CommentReaction>()
            .HasOne(r => r.User)
            .WithMany(u => u.CommentReactions)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> Replies
        builder.Entity<CommentReply>()
            .HasOne(r => r.Comment)
            .WithMany(c => c.Replies)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade); // Supprimer réponses si commentaire supprimé

        builder.Entity<CommentReply>()
            .HasOne(r => r.Author)
            .WithMany(u => u.CommentReplies)
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reply -> Reactions
        builder.Entity<CommentReplyReaction>()
            .HasOne(r => r.Reply)
            .WithMany(r => r.Reactions)
            .HasForeignKey(r => r.ReplyId)
            .OnDelete(DeleteBehavior.Cascade); // Supprimer réactions de réponse si réponse supprimée

        builder.Entity<CommentReplyReaction>()
            .HasOne(r => r.User)
            .WithMany(u => u.CommentReplyReactions)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // EventInterest -> Event
        builder.Entity<EventInterest>()
            .HasOne(ei => ei.Event)
            .WithMany(e => e.Interests)
            .HasForeignKey(ei => ei.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventInterest -> User
        builder.Entity<EventInterest>()
            .HasOne(ei => ei.User)
            .WithMany(u => u.EventInterests)
            .HasForeignKey(ei => ei.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // EventParticipation -> Event
        builder.Entity<EventParticipation>()
            .HasOne(p => p.Event)
            .WithMany(e => e.Participations)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventParticipation -> User
        builder.Entity<EventParticipation>()
            .HasOne(p => p.User)
            .WithMany(u => u.EventParticipations)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<JobOffer>()
       .HasOne(j => j.Publisher)
       .WithMany()
       .HasForeignKey(j => j.PublisherId)
       .OnDelete(DeleteBehavior.Restrict);

        // JobApplication relationships
        builder.Entity<JobApplication>()
            .HasOne(a => a.JobOffer)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobOfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JobApplication>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JobApplication>()
            .HasOne(a => a.RecommendedBy)
            .WithMany()
            .HasForeignKey(a => a.RecommendedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
