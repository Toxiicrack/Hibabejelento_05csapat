using System.ComponentModel.DataAnnotations;
using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.DTOs;

public sealed class CreateTicketRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string Location { get; init; } = string.Empty;

    [StringLength(100)]
    public string? AssetIdentifier { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

public sealed class AcceptTicketRequest
{
    [Range(1, int.MaxValue)]
    public int GroupId { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    public TicketPriority Priority { get; init; } = TicketPriority.Normál;
}

public sealed record TicketResponse(
    int Id,
    string TicketNumber,
    string Title,
    string Description,
    string Location,
    string? AssetIdentifier,
    int CategoryId,
    string CategoryName,
    TicketPriority Priority,
    TicketStatus Status,
    int ReporterId,
    int? GroupId,
    string? GroupName,
    DateTime DueDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);