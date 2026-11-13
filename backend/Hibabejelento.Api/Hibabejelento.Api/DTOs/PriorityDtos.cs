using System.ComponentModel.DataAnnotations;

namespace Hibabejelento.Api.DTOs;

public sealed class SavePriorityRequest
{
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 365)]
    public int DefaultDueDays { get; init; }

    public bool IsDefault { get; init; }
}

public sealed record PriorityResponse(
    int Id,
    string Name,
    int DefaultDueDays,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt);