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
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
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

        var location = await _db.Locations
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.LocationId &&
                    candidate.IsActive,
                cancellationToken);

        if (location is null)
        {
            return BadRequest(new
            {
                message = "A helyszín nem létezik vagy inaktív."
            });
        }

        var defaultPriority = await _db.Priorities
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.IsDefault &&
                    candidate.IsActive,
                cancellationToken);

        if (defaultPriority is null)
        {
            return Conflict(new
            {
                message = "Nincs aktív alapértelmezett prioritás beállítva."
            });
        }

        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            TicketNumber =
                $"HIB-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",

            Title = request.Title.Trim(),
            Description = request.Description.Trim(),

            // A felhasználó LocationId-t küld; a nevet a backend tölti ki.
            LocationId = location.Id,
            LocationData = location,
            Location = location.Name,

            AssetIdentifier = string.IsNullOrWhiteSpace(request.AssetIdentifier)
                ? null
                : request.AssetIdentifier.Trim(),

            CategoryId = category.Id,
            Category = category,

            // Az új, adatbázisban kezelt prioritást használjuk.
            PriorityId = defaultPriority.Id,
            PriorityData = defaultPriority,
            Priority = defaultPriority.Name,

            Status = TicketStatus.Új,
            ReporterId = userId,

            DueDate = AddWorkdays(
                now,
                defaultPriority.DefaultDueDays),

            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tickets.Add(ticket);

        _db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            Ticket = ticket,
            OldStatus = null,
            NewStatus = TicketStatus.Új,
            ChangedByUserId = userId,
            Reason = "Bejelentés létrehozása",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        var savedTicket = await GetTicketResponseQuery()
            .SingleAsync(
                candidate => candidate.Id == ticket.Id,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetByTicketNumber),
            new { ticketNumber = ticket.TicketNumber },
            ToResponse(savedTicket));
    }

    [HttpGet]
    public async Task<IActionResult> GetVisibleTickets(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.IsAdmin,
                user.GroupId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var query = GetTicketResponseQuery();

        if (!access.IsAdmin)
        {
            // A saját és a felhasználó csoportjához rendelt ticketek
            // ugyanabban a listában jelennek meg.
            query = query.Where(ticket =>
                ticket.ReporterId == userId ||
                (access.GroupId != null &&
                 ticket.GroupId == access.GroupId));
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(tickets.Select(ToResponse));
    }

    [HttpGet("{ticketNumber}")]
    public async Task<IActionResult> GetByTicketNumber(
        string ticketNumber,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.IsAdmin,
                user.GroupId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var normalizedTicketNumber =
            ticketNumber.Trim().ToUpperInvariant();

        var ticket = await GetTicketResponseQuery()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TicketNumber == normalizedTicketNumber &&
                    (access.IsAdmin ||
                     candidate.ReporterId == userId ||
                     (access.GroupId != null &&
                      candidate.GroupId == access.GroupId)),
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(ticket));
    }

    [HttpPost("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        // Csak a bejelentő vonhatja vissza a saját ticketjét.
        var ticket = await _db.Tickets
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.ReporterId == userId,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.Status == TicketStatus.Visszavonva)
        {
            return NoContent();
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        ticket.Status = TicketStatus.Visszavonva;
        ticket.IsActive = false;
        ticket.ClosedAt = now;
        ticket.UpdatedAt = now;

        _db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Visszavonva,
            ChangedByUserId = userId,
            Reason = "A bejelentő visszavonta a ticketet.",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<Ticket> GetTicketResponseQuery()
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
            ticket.LocationId,
            ticket.Location,
            ticket.AssetIdentifier,
            ticket.CategoryId,
            ticket.Category.Name,
            ticket.PriorityId,
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

    private bool TryGetCurrentUserId(out int userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }
}