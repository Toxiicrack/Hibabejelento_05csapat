using Hibabejelento.Api.Middleware.Enums;
using System.Text.RegularExpressions;

namespace Hibabejelento.Api.Middleware.Entities;

public sealed class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public int? GroupId { get; set; }

    public Group? Group { get; set; }

    public bool IsActive { get; set; } = true;

    public RegistrationStatus RegistrationStatus { get; set; }
        = RegistrationStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}