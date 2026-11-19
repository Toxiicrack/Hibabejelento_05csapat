namespace Hibabejelento.Maui.Models;

public class TicketCreation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string? AssetIdentifier { get; set; }
    public int CategoryId { get; set; }
}
