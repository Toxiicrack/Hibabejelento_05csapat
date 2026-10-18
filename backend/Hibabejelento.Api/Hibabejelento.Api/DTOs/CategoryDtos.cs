using System.ComponentModel.DataAnnotations;

namespace Hibabejelento.Api.DTOs;

public sealed class CreateCategoryRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);