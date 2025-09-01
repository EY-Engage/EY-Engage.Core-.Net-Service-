namespace EYEngage.Core.Application.Dto;

public record CommentDto
{
    /// <summary>
    /// Identifiant du commentaire
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Identifiant de l'événement auquel le commentaire appartient
    /// </summary>
    public Guid? EventId { get; init; }

    /// <summary>
    /// Identifiant de l'auteur du commentaire
    /// </summary>
    public Guid? AuthorId { get; init; }

    /// <summary>
    /// Nom complet de l'auteur
    /// </summary>
    public string AuthorFullName { get; init; } = null!;

    /// <summary>
    /// URL (relative) de la photo de profil de l'auteur
    /// </summary>
    public string? AuthorProfilePicture { get; init; }

    /// <summary>
    /// Contenu textuel du commentaire
    /// </summary>
    public string Content { get; init; } = null!;

    /// <summary>
    /// Date et heure de création
    /// </summary>
    public DateTime CreatedAt { get; init; }
    public string  EventTitle { get; init; } = null!;
}
