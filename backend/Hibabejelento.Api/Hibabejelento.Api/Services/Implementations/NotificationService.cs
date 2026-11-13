using Hibabejelento.Api.Data;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Middleware.Enums;
using Hibabejelento.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    private const int MaxMessageLength = 500;

    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public void NotifyUser(
        int userId,
        int? ticketId,
        NotificationType type,
        string message)
    {
        _db.Notifications.Add(Create(userId, ticketId, type, message));
    }

    public async Task NotifyAdminsAsync(
        int? ticketId,
        NotificationType type,
        string message,
        int? excludedUserId,
        CancellationToken cancellationToken)
    {
        var adminIds = await _db.Users
            .AsNoTracking()
            .Where(user =>
                user.IsAdmin &&
                user.IsActive &&
                user.RegistrationStatus == RegistrationStatus.Approved &&
                user.Id != excludedUserId)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(Create(adminId, ticketId, type, message));
        }
    }

    public async Task NotifyGroupMembersAsync(
        int groupId,
        int? ticketId,
        NotificationType type,
        string message,
        IReadOnlyCollection<int> excludedUserIds,
        CancellationToken cancellationToken)
    {
        var memberIds = await _db.Users
            .AsNoTracking()
            .Where(user =>
                user.GroupId == groupId &&
                user.IsActive &&
                user.RegistrationStatus == RegistrationStatus.Approved &&
                !excludedUserIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var memberId in memberIds)
        {
            _db.Notifications.Add(Create(memberId, ticketId, type, message));
        }
    }

    private static Notification Create(
        int userId,
        int? ticketId,
        NotificationType type,
        string message)
    {
        var trimmed = message.Trim();

        if (trimmed.Length > MaxMessageLength)
        {
            trimmed = trimmed[..(MaxMessageLength - 3)] + "...";
        }

        return new Notification
        {
            UserId = userId,
            TicketId = ticketId,
            Type = type,
            Message = trimmed,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}