using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Middleware.Enums;
using Hibabejelento.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/tickets")]
public sealed class AdminTicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public AdminTicketsController(
        AppDbContext db,
        INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] bool newOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var query = GetTicketQuery();

        if (newOnly)
        {
            query = query.Where(ticket => ticket.Status == TicketStatus.Új);
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(tickets.Select(ToResponse));
    }

    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(
        int id,
        AcceptTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId) ||
            !await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var ticket = await _db.Tickets
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!ticket.IsActive || ticket.Status != TicketStatus.Új)
        {
            return Conflict(new
            {
                message = "Csak aktív, új ticket fogadható el."
            });
        }

        var group = await _db.Groups
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.GroupId &&
                    candidate.IsActive,
                cancellationToken);

        if (group is null)
        {
            return BadRequest(new
            {
                message = "A csoport nem létezik vagy inaktív."
            });
        }

        var category = await _db.Categories
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.CategoryId &&
                    candidate.IsActive,
                cancellationToken);

        if (category is null)
        {
            return BadRequest(new
            {
                message = "A kategória nem létezik vagy inaktív."
            });
        }

        var priority = await _db.Priorities
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.PriorityId &&
                    candidate.IsActive,
                cancellationToken);

        if (priority is null)
        {
            return BadRequest(new
            {
                message = "A prioritás nem létezik vagy inaktív."
            });
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        ticket.GroupId = group.Id;
        ticket.Group = group;

        ticket.CategoryId = category.Id;
        ticket.Category = category;

        ticket.PriorityId = priority.Id;
        ticket.PriorityData = priority;
        ticket.Priority = priority.Name;

        ticket.Status = TicketStatus.Folyamatban;
        ticket.DueDate = AddWorkdays(now, priority.DefaultDueDays);
        ticket.UpdatedAt = now;

        _db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Folyamatban,
            ChangedByUserId = adminUserId,
            Reason = "Adminisztrátori elfogadás és csoporthoz rendelés.",
            CreatedAt = now
        });

        if (ticket.ReporterId != adminUserId)
        {
            _notifications.NotifyUser(
                ticket.ReporterId,
                ticket.Id,
                NotificationType.HibajegyElfogadva,
                $"A(z) {ticket.TicketNumber} hibajegyedet elfogadták, és a(z) " +
                $"{group.Name} csoporthoz rendelték. Prioritás: {priority.Name}.");
        }

        await _notifications.NotifyGroupMembersAsync(
            group.Id,
            ticket.Id,
            NotificationType.CsoporthozRendelve,
            $"Új hibajegy érkezett a csoportodhoz: {ticket.TicketNumber} – {ticket.Title}",
            new[] { ticket.ReporterId, adminUserId },
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<Ticket> GetTicketQuery()
    {
        return _db.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Reporter)
            .Include(ticket => ticket.Group)
            .Include(ticket => ticket.AssignedUser);
    }

    private static TicketResponse ToResponse(Ticket ticket)
    {
        return new TicketResponse(
            ticket.Id,
            ticket.TicketNumber,
            ticket.Title,
            ticket.Description,
            ticket.LocationId,
            ticket.Location,
            ticket.AssetIdentifier,
            ticket.CategoryId,
            ticket.Category.Name,
            ticket.PriorityId,
            ticket.Priority,
            ticket.Status,
            ticket.ReporterId,
            ticket.Reporter.Name,
            ticket.GroupId,
            ticket.Group == null ? null : ticket.Group.Name,
            ticket.DueDate,
            ticket.IsActive,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ClosedAt,
            ticket.AssignedUserId,
            ticket.AssignedUser == null ? null : ticket.AssignedUser.Name);
    }

    private static DateTime AddWorkdays(
        DateTime start,
        int workdays)
    {
        var result = start;

        while (workdays > 0)
        {
            result = result.AddDays(1);

            if (result.DayOfWeek is not DayOfWeek.Saturday
                and not DayOfWeek.Sunday)
            {
                workdays--;
            }
        }

        return result;
    }

    private async Task<bool> CurrentUserIsAdminAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return false;
        }

        return await _db.Users.AnyAsync(
            user =>
                user.Id == userId &&
                user.IsAdmin &&
                user.IsActive &&
                user.RegistrationStatus == RegistrationStatus.Approved,
            cancellationToken);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }
}