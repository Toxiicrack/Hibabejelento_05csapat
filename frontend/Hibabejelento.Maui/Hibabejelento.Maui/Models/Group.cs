namespace Hibabejelento.Maui.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

