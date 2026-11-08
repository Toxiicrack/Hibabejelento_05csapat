namespace Hibabejelento.Api.Middleware.Entities;

public sealed class Priority
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DefaultDueDays { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}