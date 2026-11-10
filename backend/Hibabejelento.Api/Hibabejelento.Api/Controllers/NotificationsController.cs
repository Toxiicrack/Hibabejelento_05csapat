using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        take = Math.Clamp(take, 1, 200);

        var query = _db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Take(take)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.TicketId,
                notification.Ticket == null
                    ? null
                    : notification.Ticket.TicketNumber,
                notification.Type,
                notification.Message,
                notification.IsRead,
                notification.CreatedAt,
                notification.ReadAt))
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var count = await _db.Notifications
            .CountAsync(
                notification =>
                    notification.UserId == userId &&
                    !notification.IsRead,
                cancellationToken);

        return Ok(new UnreadNotificationCountResponse(count));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var notification = await _db.Notifications
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.UserId == userId,
                cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;

        await _db.Notifications
            .Where(notification =>
                notification.UserId == userId &&
                !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(
                        notification => notification.ReadAt,
                        (DateTime?)now),
                cancellationToken);

        return NoContent();
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }
}