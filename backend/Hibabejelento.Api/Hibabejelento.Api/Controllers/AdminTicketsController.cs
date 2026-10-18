using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Middleware.Enums;
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

    public AdminTicketsController(AppDbContext db)
    {
        _db = db;
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

        var groupIsActive = await _db.Groups.AnyAsync(
            group => group.Id == request.GroupId && group.IsActive,
            cancellationToken);

        if (!groupIsActive)
        {
            return BadRequest(new
            {
                message = "A csoport nem létezik vagy inaktív."
            });
        }

        var categoryIsActive = await _db.Categories.AnyAsync(
            category =>
                category.Id == request.CategoryId &&
                category.IsActive,
            cancellationToken);

        if (!categoryIsActive)
        {
            return BadRequest(new
            {
                message = "A kategória nem létezik vagy inaktív."
            });
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        ticket.GroupId = request.GroupId;
        ticket.CategoryId = request.CategoryId;
        ticket.Priority = request.Priority;
        ticket.Status = TicketStatus.Folyamatban;
        ticket.DueDate = AddWorkdays(
            now,
            GetDueWorkdays(request.Priority));
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

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<Ticket> GetTicketQuery()
    {
        return _db.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Group);
    }

    private static TicketResponse ToResponse(Ticket ticket)
    {
        return new TicketResponse(
            ticket.Id,
            ticket.TicketNumber,
            ticket.Title,
            ticket.Description,
            ticket.Location,
            ticket.AssetIdentifier,
            ticket.CategoryId,
            ticket.Category.Name,
            ticket.Priority,
            ticket.Status,
            ticket.ReporterId,
            ticket.GroupId,
            ticket.Group == null ? null : ticket.Group.Name,
            ticket.DueDate,
            ticket.IsActive,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ClosedAt);
    }

    private static int GetDueWorkdays(TicketPriority priority)
    {
        return priority switch
        {
            TicketPriority.Sürgős => 1,
            TicketPriority.Magas => 3,
            TicketPriority.Normál => 5,
            TicketPriority.Alacsony => 10,
            _ => 5
        };
    }

    private static DateTime AddWorkdays(DateTime start, int workdays)
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