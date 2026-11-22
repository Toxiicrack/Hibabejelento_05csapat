namespace Hibabejelento.Maui.Models;

public class Priority
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultDueDays { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
