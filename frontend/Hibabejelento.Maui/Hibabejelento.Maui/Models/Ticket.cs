
namespace Hibabejelento.Maui.Models;

public class Ticket
{
    public int Id { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string AssetIdentifier { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    // Például: "Alacsony", "Normál", "Magas"
    public string Priority { get; set; } = string.Empty;

    // Például: "Új", "Folyamatban", "Lezárt"
    public string Status { get; set; } = string.Empty;

    public int ReporterId { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}
