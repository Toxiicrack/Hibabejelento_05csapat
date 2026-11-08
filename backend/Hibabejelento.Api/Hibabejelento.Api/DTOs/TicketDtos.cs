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

    [Range(1, int.MaxValue)]
    public int LocationId { get; init; }

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

    [Range(1, int.MaxValue)]
    public int PriorityId { get; init; }
}

public sealed class CreateTicketNoteRequest
{
    [Required]
    [StringLength(5000)]
    public string Message { get; init; } = string.Empty;
}

public sealed record TicketNoteResponse(
    int Id,
    int TicketId,
    int UserId,
    string UserName,
    string Message,
    DateTime CreatedAt);

public sealed record TicketResponse(
    int Id,
    string TicketNumber,
    string Title,
    string Description,
    int LocationId,
    string Location,
    string? AssetIdentifier,
    int CategoryId,
    string CategoryName,
    int PriorityId,
    string Priority,
    TicketStatus Status,
    int ReporterId,
    int? GroupId,
    string? GroupName,
    DateTime DueDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt,
    int? AssignedUserId,
    string? AssignedUserName);