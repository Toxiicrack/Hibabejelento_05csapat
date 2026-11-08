using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.Middleware.Entities;

public sealed class Ticket
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Megjelenítési pillanatkép; az új bejelentések helyszínét LocationId választja ki.
    public string Location { get; set; } = string.Empty;

    public int LocationId { get; set; }

    public Location LocationData { get; set; } = null!;

    public string? AssetIdentifier { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    // Megjelenítési pillanatkép; az aktuális prioritás a PriorityId-hoz kapcsolódik.
    public string Priority { get; set; } = string.Empty;

    public int PriorityId { get; set; }

    public Priority PriorityData { get; set; } = null!;

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