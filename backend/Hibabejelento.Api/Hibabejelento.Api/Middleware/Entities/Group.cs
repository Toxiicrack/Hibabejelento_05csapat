namespace Hibabejelento.Api.Middleware.Entities;

public sealed class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}