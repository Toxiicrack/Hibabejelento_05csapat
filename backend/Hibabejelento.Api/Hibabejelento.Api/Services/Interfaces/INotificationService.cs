using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.Services.Interfaces;

public interface INotificationService
{
    void NotifyUser(
        int userId,
        int? ticketId,
        NotificationType type,
        string message);

    Task NotifyAdminsAsync(
        int? ticketId,
        NotificationType type,
        string message,
        int? excludedUserId,
        CancellationToken cancellationToken);

    Task NotifyGroupMembersAsync(
        int groupId,
        int? ticketId,
        NotificationType type,
        string message,
        IReadOnlyCollection<int> excludedUserIds,
        CancellationToken cancellationToken);
}