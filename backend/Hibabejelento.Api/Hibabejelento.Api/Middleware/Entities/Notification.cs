using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.Middleware.Entities;

public sealed class Notification
{
    public int Id { get; set; }

    // Az értesítés címzettje.
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Null, ha az értesítés nem hibajegyhez kapcsolódik (pl. új regisztráció).
    public int? TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public NotificationType Type { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}