using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.DTOs;

public sealed record NotificationResponse(
    int Id,
    int? TicketId,
    string? TicketNumber,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record UnreadNotificationCountResponse(int Count);