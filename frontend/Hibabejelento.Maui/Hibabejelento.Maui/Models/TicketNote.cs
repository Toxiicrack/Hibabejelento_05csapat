namespace Hibabejelento.Maui.Models;

public class TicketNote
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ClosureTypeId { get; set; }
}

public class TicketNoteRequest
{
    public string Message { get; set; } = string.Empty;
}

public class TicketCloseRequest
{
    public int ClosureTypeId { get; set; }
    public string Resolution { get; set; } = string.Empty;
}
