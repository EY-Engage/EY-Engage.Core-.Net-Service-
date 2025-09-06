using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.EventDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EYEngage.Core.Application.Services
{
    public class EventService : IEventService
    {
        private readonly EYEngageDbContext _db;
        private readonly IEmailService _mail;
        private readonly IWebHostEnvironment _env;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        public EventService(
            EYEngageDbContext db,
            IEmailService mail,
            IWebHostEnvironment env
            )
        {
            _db = db;
            _mail = mail;
            _env = env;
        }

        public async Task<EventDto> CreateEventAsync(Guid organizerId, CreateEventDto dto)
        {
            var actualOrganizer = await ResolveUserId(organizerId);

            string? imagePath = null;
            if (dto.ImageFile != null)
            {
                imagePath = await SaveEventImageAsync(dto.ImageFile);
            }

            var ev = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                Date = dto.Date,
                Location = dto.Location,
                OrganizerId = actualOrganizer,
                ImagePath = imagePath,
                Status = EventStatus.Pending
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            // NOTIFICATION: Événement créé

            return Map(ev, actualOrganizer);
        }

        private async Task<string> SaveEventImageAsync(IFormFile file)
        {
            ValidateFile(file);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var folderPath = Path.Combine(_env.WebRootPath, "events");
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/events/{fileName}";
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null)
                throw new ValidationException("Aucun fichier reçu");

            if (file.Length > MaxFileSize)
                throw new ValidationException($"Taille maximale autorisée : {MaxFileSize / 1024 / 1024} Mo");

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new ValidationException($"Extensions autorisées : {string.Join(", ", AllowedExtensions)}");
        }

        private async Task<Guid> ResolveUserId(Guid incoming)
        {
            if (incoming != Guid.Empty) return incoming;
            return await _db.Users.Select(u => u.Id).FirstAsync();
        }

        public async Task<IEnumerable<EventDto>> GetEventsByStatusAsync(EventStatus status, Guid userId, string? department = null)
        {
            var query = _db.Events
                .Include(e => e.Organizer)
                .Include(e => e.Interests)
                .Include(e => e.Participations)
                .Where(e => e.Status == status);

            if (!string.IsNullOrEmpty(department))
            {
                if (Enum.TryParse<Department>(department, out var deptEnum))
                {
                    query = query.Where(e => e.Organizer.Department == deptEnum);
                }
                else
                {
                    throw new ValidationException("Département invalide");
                }
            }

            var events = await query
                .OrderBy(e => e.Date)
                .ToListAsync();

            return events.Select(e => Map(e, userId));
        }

        public Task RequestParticipationAsync(Guid eventId, Guid userId)
          => UpsertParticipation(eventId, userId, ParticipationStatus.Pending);

        private async Task UpsertParticipation(Guid eventId, Guid userId, ParticipationStatus target)
        {
            var actualUser = await ResolveUserId(userId);

            var p = await _db.EventParticipations
                             .Include(p => p.Event)
                             .Include(p => p.User)
                             .FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == actualUser);

            if (p == null)
            {
                p = new EventParticipation
                {
                    EventId = eventId,
                    UserId = actualUser,
                    Status = target
                };
                _db.EventParticipations.Add(p);
            }
            else
            {
                p.Status = target;
            }

            await _db.SaveChangesAsync();

            // NOTIFICATION: Demande de participation
            if (target == ParticipationStatus.Pending)
            {
            }
        }

        public async Task ApproveParticipationAsync(Guid participationId, Guid approvedById)
        {
            var p = await _db.EventParticipations
                 .Include(x => x.User)
                 .Include(x => x.Event)
                 .Include(x => x.ApprovedBy)
                 .FirstAsync(x => x.Id == participationId);

            p.Status = ParticipationStatus.Approved;
            p.DecidedAt = DateTime.UtcNow;
            p.ApprovedById = approvedById;
            await _db.SaveChangesAsync();

            // NOTIFICATION: Participation approuvée

            var subject = $"Participation confirmée : {p.Event.Title}";
            var body = $@"
                Bonjour {p.User.FullName},<br/>
                Vous êtes accepté·e à l'événement <b>{p.Event.Title}</b> du {p.Event.Date:dd/MM/yyyy} à {p.Event.Location}.<br/>
                Cordialement,<br/>L'équipe EY Engage";

            await _mail.SendEmailAsync(p.User.Email, subject, body);
        }

        public async Task<IReadOnlyList<ParticipationRequestDto>> GetParticipationRequestsAsync(Guid eventId)
        {
            return await _db.EventParticipations
                .Where(p => p.EventId == eventId && p.Status == ParticipationStatus.Pending)
                .Include(p => p.User)
                .Select(p => new ParticipationRequestDto(
                    p.Id,
                    p.UserId,
                    p.User.FullName,
                    p.User.Email,
                    p.User.ProfilePicture,
                    p.RequestedAt
                ))
                .ToListAsync();
        }


        public async Task CommentAsync(Guid eventId, Guid authorId, string content)
        {
            var actualUser = await ResolveUserId(authorId);
            var eventEntity = await _db.Events.FindAsync(eventId);

            if (eventEntity == null)
                throw new Exception("Événement non trouvé");

            var comment = new Comment
            {
                EventId = eventId,
                AuthorId = actualUser,
                Content = content
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // NOTIFICATION: Commentaire créé
        }

        public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid eventId)
        {
            return await _db.Comments
                .Where(c => c.EventId == eventId)
                .Include(c => c.Author)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    EventId = c.EventId,
                    AuthorId = (Guid)c.AuthorId,
                    AuthorFullName = c.Author.FullName,
                    AuthorProfilePicture = c.Author.ProfilePicture,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task UpdateEventStatusAsync(Guid eventId, EventStatus status, Guid approvedById)
        {
            var ev = await _db.Events
                .Include(e => e.Organizer)
                .Include(e => e.ApprovedBy)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) throw new KeyNotFoundException($"Event {eventId} not found");

            ev.Status = status;

            if (status == EventStatus.Approved)
            {
                ev.ApprovedById = approvedById;
                // NOTIFICATION: Événement approuvé
            }
            else if (status == EventStatus.Rejected)
            {
                ev.ApprovedById = approvedById;
                // NOTIFICATION: Événement rejeté
            }

            await _db.SaveChangesAsync();
        }
        // EventService.cs
        public async Task<UserProfileEventsDto> GetUserProfileEventsAsync(Guid userId)
        {
            // Exécution séquentielle des requêtes
            var organizedEvents = await _db.Events
                .Where(e => e.OrganizerId == userId)
                .ToListAsync();

            var participations = await _db.EventParticipations
                .Where(p => p.UserId == userId && p.Status == ParticipationStatus.Approved)
                .Include(p => p.Event)
                .ToListAsync();

            var approvedEvents = await _db.Events
                .Where(e => e.ApprovedById == userId)
                .ToListAsync();

            var approvedParticipations = await _db.EventParticipations
                .Where(p => p.ApprovedById == userId)
                .Include(p => p.Event)
                .Include(p => p.User)
                .ToListAsync();

            var comments = await _db.Comments
                .Where(c => c.AuthorId == userId)
                .Include(c => c.Event)
                .ToListAsync();

            return new UserProfileEventsDto
            {
                OrganizedEvents = organizedEvents.Select(MapToMiniDto).ToList(),
                ParticipatedEvents = participations.Select(p => MapToMiniDto(p.Event)).ToList(),
                ApprovedEvents = approvedEvents.Select(MapToMiniDto).ToList(),
                ApprovedParticipations = approvedParticipations.Select(ap =>
                    new ApprovedParticipationDto(
                        ap.Id,
                        ap.EventId,
                        ap.Event?.Title ?? "Titre non disponible",
                        ap.User?.FullName ?? "Participant inconnu",
                        ap.User?.ProfilePicture
                    )
                ).ToList(),
                Comments = comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    EventId = c.EventId,
                    EventTitle = c.Event?.Title ?? "Événement inconnu",
                    Content = c.Content
                }).ToList()
            };
        }
        private EventMiniDto MapToMiniDto(Event e) => new(
            e.Id,
            e.Title,
            e.Date,
            e.Location,
            e.ImagePath
        );

        public async Task ToggleInterestAsync(Guid eventId, Guid userId)
        {
            var actualUser = await ResolveUserId(userId);
            var existing = await _db.EventInterests
                .FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == actualUser);

            if (existing != null)
            {
                _db.EventInterests.Remove(existing);
            }
            else
            {
                _db.EventInterests.Add(new EventInterest
                {
                    EventId = eventId,
                    UserId = actualUser
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<UserDto>> GetInterestedUsersAsync(Guid eventId)
        {
            return await _db.EventInterests
                .Where(i => i.EventId == eventId)
                .Select(i => new UserDto
                {
                    Id = i.User.Id,
                    FullName = i.User.FullName,
                    Email = i.User.Email,
                    ProfilePicture = i.User.ProfilePicture
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<UserDto>> GetParticipantsAsync(Guid eventId)
        {
            return await _db.EventParticipations
                .Where(p => p.EventId == eventId && p.Status == ParticipationStatus.Approved)
                .Include(p => p.User)
                .Select(p => new UserDto
                {
                    Id = p.User.Id,
                    FullName = p.User.FullName,
                    Email = p.User.Email,
                    ProfilePicture = p.User.ProfilePicture
                })
                .ToListAsync();
        }

        public async Task<bool> IsUserInterested(Guid eventId, Guid userId)
        {
            return await _db.EventInterests
                .AnyAsync(i => i.EventId == eventId && i.UserId == userId);
        }

        public async Task<EventParticipation> GetUserParticipationStatus(Guid eventId, Guid userId)
        {
            return await _db.EventParticipations
                .FirstOrDefaultAsync(p =>
                    p.EventId == eventId &&
                    p.UserId == userId);
        }

        public async Task RejectParticipationAsync(Guid participationId)
        {
            var participation = await _db.EventParticipations
                                         .Include(p => p.User)
                                         .Include(p => p.Event)
                                         .Include(p => p.ApprovedBy)
                                         .FirstOrDefaultAsync(p => p.Id == participationId);

            if (participation == null)
                throw new Exception("Demande non trouvée");

            participation.Status = ParticipationStatus.Rejected;
            participation.DecidedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var subject = $"Participation refusée : {participation.Event.Title}";
            var body = $@"
        Bonjour {participation.User.FullName},<br/>
        Votre demande de participation à l'événement <b>{participation.Event.Title}</b> a été <span style='color:red;'>refusée</span>.<br/>
        Cordialement,<br/>L'équipe EY Engage";

            await _mail.SendEmailAsync(participation.User.Email, subject, body);
        }

        public async Task ReactToCommentAsync(Guid commentId, Guid userId, string emoji)
        {
            var actualUser = await ResolveUserId(userId);
            var existing = await _db.CommentReactions.FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == actualUser);

            if (existing != null)
            {
                existing.Emoji = emoji;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CommentReactions.Add(new CommentReaction
                {
                    CommentId = commentId,
                    UserId = actualUser,
                    Emoji = emoji
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<CommentReactionDto>> GetReactionsForCommentAsync(Guid commentId)
        {
            return await _db.CommentReactions
                .Where(r => r.CommentId == commentId)
                .Include(r => r.User)
                .Select(r => new CommentReactionDto(
                    r.Id,
                    r.CommentId,
                    r.UserId,
                    r.User.FullName,
                    r.User.ProfilePicture,
                    r.Emoji,
                    r.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task ReplyToCommentAsync(Guid commentId, Guid authorId, string content)
        {
            var actualUser = await ResolveUserId(authorId);
            _db.CommentReplies.Add(new CommentReply
            {
                CommentId = commentId,
                AuthorId = actualUser,
                Content = content
            });
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<CommentReplyDto>> GetRepliesForCommentAsync(Guid commentId)
        {
            return await _db.CommentReplies
                .Where(r => r.CommentId == commentId)
                .Include(r => r.Author)
                .Select(r => new CommentReplyDto(
                    r.Id,
                    r.CommentId,
                    r.AuthorId,
                    r.Author.FullName,
                    r.Author.ProfilePicture,
                    r.Content,
                    r.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task DeleteCommentAsync(Guid commentId, Guid userId)
        {
            var comment = await _db.Comments
                .Include(c => c.Reactions)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.Reactions)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.AuthorId == userId);

            if (comment == null)
                throw new UnauthorizedAccessException("Non autorisé à supprimer ce commentaire");

            // Supprimer réactions des réponses
            foreach (var reply in comment.Replies)
            {
                _db.CommentReplyReactions.RemoveRange(reply.Reactions);
            }

            // Supprimer réactions du commentaire
            _db.CommentReactions.RemoveRange(comment.Reactions);

            // Supprimer réponses
            _db.CommentReplies.RemoveRange(comment.Replies);

            // Supprimer le commentaire
            _db.Comments.Remove(comment);

            await _db.SaveChangesAsync();
        }


        public async Task ReactToReplyAsync(Guid replyId, Guid userId, string emoji)
        {
            var actualUser = await ResolveUserId(userId);
            var existing = await _db.CommentReplyReactions
                .FirstOrDefaultAsync(r => r.ReplyId == replyId && r.UserId == actualUser);

            if (existing != null)
            {
                existing.Emoji = emoji;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CommentReplyReactions.Add(new CommentReplyReaction
                {
                    ReplyId = replyId,
                    UserId = actualUser,
                    Emoji = emoji
                });
            }

            await _db.SaveChangesAsync();
        }
        public async Task DeleteReplyAsync(Guid replyId, Guid userId)
        {
            var reply = await _db.CommentReplies
                .Include(r => r.Reactions)
                .FirstOrDefaultAsync(r => r.Id == replyId && r.AuthorId == userId);

            if (reply == null)
                throw new UnauthorizedAccessException("Non autorisé à supprimer cette réponse");

            // Supprimer réactions de la réponse
            _db.CommentReplyReactions.RemoveRange(reply.Reactions);

            // Supprimer la réponse
            _db.CommentReplies.Remove(reply);

            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<object>> GetReplyReactionsAsync(Guid replyId)
        {
            var reactions = await _db.CommentReplyReactions
                .Where(r => r.ReplyId == replyId)
                .Include(r => r.User)
                .ToListAsync();

            return reactions.Select(r => new {
                id = r.Id,
                emoji = r.Emoji,
                userId = r.User.Id,
                userFullName = r.User.FullName,
                userProfilePicture = r.User.ProfilePicture
            });
        }
        public async Task DeleteEventAsync(Guid eventId, Guid requesterId)
        {
            var existingEvent = await _db.Events
                .Include(e => e.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.Reactions)
                .Include(e => e.Comments)
                    .ThenInclude(c => c.Reactions)
                .Include(e => e.Participations)
                .Include(e => e.Interests)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (existingEvent == null)
                throw new KeyNotFoundException("Event not found.");

            // Vérifier autorisation ici (à implémenter)

            // Supprimer CommentReplyReactions
            var allReplyReactions = existingEvent.Comments
                .SelectMany(c => c.Replies)
                .SelectMany(r => r.Reactions)
                .ToList();
            _db.CommentReplyReactions.RemoveRange(allReplyReactions);

            // Supprimer CommentReplies
            var allReplies = existingEvent.Comments
                .SelectMany(c => c.Replies)
                .ToList();
            _db.CommentReplies.RemoveRange(allReplies);

            // Supprimer CommentReactions
            var allCommentReactions = existingEvent.Comments
                .SelectMany(c => c.Reactions)
                .ToList();
            _db.CommentReactions.RemoveRange(allCommentReactions);

            // Supprimer Comments
            _db.Comments.RemoveRange(existingEvent.Comments);

            // Supprimer Participations
            _db.EventParticipations.RemoveRange(existingEvent.Participations);

            // Supprimer Interests
            _db.EventInterests.RemoveRange(existingEvent.Interests);

            // Supprimer Event
            _db.Events.Remove(existingEvent);

            await _db.SaveChangesAsync();
        }

        // Ajouter ces méthodes dans EventService.cs
        public async Task<EventAnalyticsDto> GetEventAnalyticsAsync(Guid? userId = null, string? department = null)
        {
            var query = _db.Events
                .Include(e => e.Participations)
                .Include(e => e.Interests)
                .Include(e => e.Organizer)
                .Where(e => e.Status == EventStatus.Approved);

            // Ajout du filtre par département si userId est fourni
            if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    query = query.Where(e => e.Organizer.Department == user.Department);
                }
            }
            else if (!string.IsNullOrEmpty(department))
            {
                if (Enum.TryParse<Department>(department, out var deptEnum))
                {
                    query = query.Where(e => e.Organizer.Department == deptEnum);
                }
                else
                {
                    throw new ValidationException("Département invalide");
                }
            }

            var events = await query.ToListAsync();

            // Calcul des statistiques
            var totalEvents = events.Count;
            var totalParticipants = events.Sum(e => e.Participations.Count(p => p.Status == ParticipationStatus.Approved));
            var totalInterests = events.Sum(e => e.Interests.Count);
            var avgParticipationPerEvent = totalEvents > 0 ? totalParticipants / (double)totalEvents : 0;
            var participationRate = totalEvents > 0 ? totalParticipants / (double)(totalParticipants + totalInterests) : 0;

            // Top 5 événements les plus populaires
            var popularEvents = events
                .OrderByDescending(e => e.Participations.Count + e.Interests.Count)
                .Take(5)
                .Select(e => new PopularEventDto(
                    e.Id,
                    e.Title,
                    e.Date,
                    e.Participations.Count(p => p.Status == ParticipationStatus.Approved),
                    e.Interests.Count
                ))
                .ToList();

            // Statistiques par département
            var departmentStats = Enum.GetValues(typeof(Department))
                .Cast<Department>()
                .Select(d => new DepartmentStatsDto(
                    d.ToString(),
                    d.ToString(),
                    events.Count(e => e.Organizer.Department == d),
                    events.Sum(e => e.Organizer.Department == d ?
                        e.Participations.Count(p => p.Status == ParticipationStatus.Approved) : 0),
                    events.Sum(e => e.Organizer.Department == d ? e.Interests.Count : 0)
                ))
                .ToList();

            // Évolution mensuelle
            var monthlyStats = events
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new MonthlyStatsDto(
                    g.Key.Year,
                    g.Key.Month,
                    g.Count(),
                    g.Sum(e => e.Participations.Count(p => p.Status == ParticipationStatus.Approved)),
                    g.Sum(e => e.Interests.Count)
                ))
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToList();

            return new EventAnalyticsDto(
                totalEvents,
                totalParticipants,
                totalInterests,
                avgParticipationPerEvent,
                participationRate,
                popularEvents,
                departmentStats,
                monthlyStats
            );
        }


        public async Task<IEnumerable<EventTrendDto>> GetEventTrendsAsync(Guid? userId = null, string? department = null)
        {
            var query = _db.Events
                .Include(e => e.Participations)
                .Include(e => e.Interests)
                .Include(e => e.Organizer)
                .Where(e => e.Status == EventStatus.Approved);

            // Ajout du filtre par département si userId est fourni
            if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    query = query.Where(e => e.Organizer.Department == user.Department);
                }
            }
            else if (!string.IsNullOrEmpty(department))
            {
                if (Enum.TryParse<Department>(department, out var deptEnum))
                {
                    query = query.Where(e => e.Organizer.Department == deptEnum);
                }
                else
                {
                    throw new ValidationException("Département invalide");
                }
            }


            var events = await query.ToListAsync();

            return events
                .Select(e => new EventTrendDto(
                    e.Id,
                    e.Title,
                    e.Date,
                    e.Participations.Count(p => p.Status == ParticipationStatus.Approved),
                    e.Interests.Count,
                    (double)e.Participations.Count(p => p.Status == ParticipationStatus.Approved) /
                        (e.Interests.Count > 0 ? e.Interests.Count : 1) * 100
                ))
                .OrderByDescending(t => t.Date)
                .ToList();
        }


        private EventDto Map(Event e, Guid currentUserId) => new(
            e.Id,
            e.Title,
            e.Description,
            e.Date,
            e.Location,
            e.ImagePath,
            e.Status,
            e.Interests.Count,
            e.Participations.Count(p => p.Status == ParticipationStatus.Approved),
            e.Interests.Any(i => i.UserId == currentUserId),
            e.Participations
                .Where(p => p.UserId == currentUserId)
                .Select(p => p.Status.ToString())
                .FirstOrDefault(),
            e.Organizer?.FullName ?? "Utilisateur supprimé",
            e.Organizer?.Department.ToString() ?? "Inconnu"
        );
    }
}