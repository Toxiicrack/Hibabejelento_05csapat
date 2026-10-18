using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.Middleware.Entities;

public sealed class Ticket
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string? AssetIdentifier { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public TicketPriority Priority { get; set; } = TicketPriority.Normál;

    public TicketStatus Status { get; set; } = TicketStatus.Új;

    public int ReporterId { get; set; }

    public User Reporter { get; set; } = null!;

    public int? GroupId { get; set; }

    public Group? Group { get; set; }

    public int? AssignedUserId { get; set; }

    public User? AssignedUser { get; set; }

    public DateTime DueDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<TicketStatusHistory> StatusHistory { get; set; }
        = new List<TicketStatusHistory>();
}