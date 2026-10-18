using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.Middleware.Entities;

public sealed class TicketStatusHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket Ticket { get; set; } = null!;

    public TicketStatus? OldStatus { get; set; }

    public TicketStatus NewStatus { get; set; }

    public int ChangedByUserId { get; set; }

    public User ChangedByUser { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}