namespace Hibabejelento.Maui.Models;

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TransitionDisplay =>
        $"{(string.IsNullOrWhiteSpace(OldStatus) ? "Kezdeti" : OldStatus)} → {NewStatus}";
}
