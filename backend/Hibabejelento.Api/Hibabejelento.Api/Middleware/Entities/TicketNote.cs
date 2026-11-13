namespace Hibabejelento.Api.Middleware.Entities;

public sealed class TicketNote
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    // Csak a lezáráskor létrehozott végső bejegyzéshez tartozik.
    // Null, ha a bejegyzés nem ticketlezárás.
    public int? ClosureTypeId { get; set; }

    public DateTime CreatedAt { get; set; }
}