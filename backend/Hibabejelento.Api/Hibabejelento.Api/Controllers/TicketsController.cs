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

            LocationId = location.Id,
            LocationData = location,
            Location = location.Name,

            AssetIdentifier = string.IsNullOrWhiteSpace(request.AssetIdentifier)
                ? null
                : request.AssetIdentifier.Trim(),

            CategoryId = category.Id,
            Category = category,

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

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyTickets(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userExists = await _db.Users
            .AnyAsync(
                user =>
                    user.Id == userId &&
                    user.IsActive &&
                    user.RegistrationStatus == RegistrationStatus.Approved,
                cancellationToken);

        if (!userExists)
        {
            return Unauthorized();
        }

        var tickets = await GetTicketResponseQuery()
            .Where(ticket => ticket.ReporterId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(tickets.Select(ToResponse));
    }

    [HttpGet("maintenance")]
    public async Task<IActionResult> GetMaintenanceTickets(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        if (access.GroupId is null || !access.GroupIsActive)
        {
            return Forbid();
        }

        var tickets = await GetTicketResponseQuery()
            .Where(ticket => ticket.GroupId == access.GroupId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(tickets.Select(ToResponse));
    }

    [HttpPost("{id:int}/take")]
    public async Task<IActionResult> TakeTicket(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        if (access.GroupId is null || !access.GroupIsActive)
        {
            return Forbid();
        }

        var ticket = await _db.Tickets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.GroupId == access.GroupId,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!ticket.IsActive ||
            ticket.Status != TicketStatus.Folyamatban)
        {
            return Conflict(new
            {
                message = "Csak aktív, folyamatban lévő ticket vehető fel."
            });
        }

        if (ticket.AssignedUserId is not null)
        {
            return Conflict(new
            {
                message = "Ezt a ticketet már felvette egy karbantartó."
            });
        }

        var now = DateTime.UtcNow;

        var updatedRows = await _db.Tickets
            .Where(candidate =>
                candidate.Id == id &&
                candidate.GroupId == access.GroupId &&
                candidate.IsActive &&
                candidate.Status == TicketStatus.Folyamatban &&
                candidate.AssignedUserId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        candidate => candidate.AssignedUserId,
                        (int?)userId)
                    .SetProperty(
                        candidate => candidate.UpdatedAt,
                        now),
                cancellationToken);

        if (updatedRows == 0)
        {
            return Conflict(new
            {
                message = "Ezt a ticketet időközben már felvette valaki, vagy már nem vehető fel."
            });
        }

        var updatedTicket = await GetTicketResponseQuery()
            .SingleAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        return Ok(ToResponse(updatedTicket));
    }

    [HttpGet("{id:int}/notes")]
    public async Task<IActionResult> GetTicketNotes(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var ticketAccess = await _db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id)
            .Select(ticket => new
            {
                ticket.ReporterId,
                ticket.GroupId,
                ticket.AssignedUserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticketAccess is null)
        {
            return NotFound();
        }

        var isReporter = ticketAccess.ReporterId == userId;
        var isAssignedUser = ticketAccess.AssignedUserId == userId;
        var isGroupMember =
            access.GroupId is not null &&
            access.GroupIsActive &&
            ticketAccess.GroupId == access.GroupId;

        if (!isReporter && !isAssignedUser && !isGroupMember)
        {
            return NotFound();
        }

        var notes = await _db.TicketNotes
            .AsNoTracking()
            .Where(note => note.TicketId == id)
            .OrderBy(note => note.CreatedAt)
            .ThenBy(note => note.Id)
            .Select(note => new TicketNoteResponse(
                note.Id,
                note.TicketId,
                note.UserId,
                note.User.Name,
                note.Message,
                note.CreatedAt,
                note.ClosureTypeId))
            .ToListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpPost("{id:int}/notes")]
    public async Task<IActionResult> CreateTicketNote(
        int id,
        CreateTicketNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var ticket = await _db.Tickets
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        var isReporter = ticket.ReporterId == userId;
        var isAssignedUser = ticket.AssignedUserId == userId;

        if (!isReporter && !isAssignedUser)
        {
            return NotFound();
        }

        if (ticket.Status is TicketStatus.Lezárt or TicketStatus.Visszavonva)
        {
            return Conflict(new
            {
                message = "Lezárt vagy visszavont tickethez nem lehet bejegyzést írni."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                message = "A bejegyzés szövege nem lehet üres."
            });
        }

        var message = request.Message.Trim();

        if (message.Length > 5000)
        {
            return BadRequest(new
            {
                message = "A bejegyzés legfeljebb 5000 karakter lehet."
            });
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        // Az Új ticketen a bejelentő még adminisztrátori elfogadás előtt is
        // írhat; ettől a státusz Új marad.
        if (ticket.Status != TicketStatus.Új)
        {
            ticket.Status = isReporter
                ? TicketStatus.Folyamatban
                : TicketStatus.VálaszraVár;
        }

        ticket.UpdatedAt = now;

        var note = new TicketNote
        {
            TicketId = ticket.Id,
            UserId = userId,
            Message = message,
            CreatedAt = now
        };

        _db.TicketNotes.Add(note);

        if (ticket.Status != oldStatus)
        {
            _db.TicketStatusHistory.Add(new TicketStatusHistory
            {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = ticket.Status,
                ChangedByUserId = userId,
                Reason = isReporter
                    ? "A bejelentő új bejegyzést írt."
                    : "A ticketet felvevő karbantartó új bejegyzést írt.",
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var response = await _db.TicketNotes
            .AsNoTracking()
            .Where(candidate => candidate.Id == note.Id)
            .Select(candidate => new TicketNoteResponse(
                candidate.Id,
                candidate.TicketId,
                candidate.UserId,
                candidate.User.Name,
                candidate.Message,
                candidate.CreatedAt,
                candidate.ClosureTypeId))
            .SingleAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetTicketNotes),
            new { id },
            response);
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> CloseTicket(
        int id,
        CloseTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var ticket = await _db.Tickets
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.AssignedUserId != userId)
        {
            return Forbid();
        }

        if (!ticket.IsActive ||
            ticket.Status is not TicketStatus.Folyamatban
                and not TicketStatus.VálaszraVár
                and not TicketStatus.Újranyitva)
        {
            return Conflict(new
            {
                message = "Ez a ticket jelenlegi állapotában nem zárható le."
            });
        }

        if (!Enum.IsDefined(
                typeof(TicketClosureType),
                request.ClosureTypeId))
        {
            return BadRequest(new
            {
                message = "Érvénytelen lezárási típus."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Resolution))
        {
            return BadRequest(new
            {
                message = "A lezáró bejegyzés nem lehet üres."
            });
        }

        var resolution = request.Resolution.Trim();

        if (resolution.Length > 5000)
        {
            return BadRequest(new
            {
                message = "A lezáró bejegyzés legfeljebb 5000 karakter lehet."
            });
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        ticket.Status = TicketStatus.Lezárt;
        ticket.ClosedAt = now;
        ticket.UpdatedAt = now;

        _db.TicketNotes.Add(new TicketNote
        {
            TicketId = ticket.Id,
            UserId = userId,
            Message = resolution,
            ClosureTypeId = request.ClosureTypeId,
            CreatedAt = now
        });

        _db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Lezárt,
            ChangedByUserId = userId,
            Reason = $"Lezárás típusa: {GetClosureTypeName(request.ClosureTypeId)}",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            ticket.Id,
            ticket.Status,
            ticket.ClosedAt,
            request.ClosureTypeId
        });
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> ReopenTicket(
        int id,
        ReopenTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var ticket = await _db.Tickets
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.ReporterId != userId)
        {
            return Forbid();
        }

        if (!ticket.IsActive || ticket.Status != TicketStatus.Lezárt)
        {
            return Conflict(new
            {
                message = "Csak aktív, lezárt ticket nyitható újra."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new
            {
                message = "Az újranyitás indoklása nem lehet üres."
            });
        }

        var reason = request.Reason.Trim();

        if (reason.Length > 5000)
        {
            return BadRequest(new
            {
                message = "Az indoklás legfeljebb 5000 karakter lehet."
            });
        }

        var now = DateTime.UtcNow;
        var oldStatus = ticket.Status;

        ticket.Status = TicketStatus.Újranyitva;
        ticket.ClosedAt = null;
        ticket.UpdatedAt = now;

        _db.TicketNotes.Add(new TicketNote
        {
            TicketId = ticket.Id,
            UserId = userId,
            Message = $"Újranyitás indoklása: {reason}",
            CreatedAt = now
        });

        _db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Újranyitva,
            ChangedByUserId = userId,
            Reason = reason,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            ticket.Id,
            ticket.Status,
            ticket.UpdatedAt
        });
    }

    [HttpGet("{id:int}/status-history")]
    public async Task<IActionResult> GetStatusHistory(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var access = await GetCurrentUserAccessAsync(
            userId,
            cancellationToken);

        if (access is null)
        {
            return Unauthorized();
        }

        var ticketAccess = await _db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id)
            .Select(ticket => new
            {
                ticket.ReporterId,
                ticket.GroupId,
                ticket.AssignedUserId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticketAccess is null)
        {
            return NotFound();
        }

        var isReporter = ticketAccess.ReporterId == userId;
        var isAssignedUser = ticketAccess.AssignedUserId == userId;
        var isGroupMember =
            access.GroupId is not null &&
            access.GroupIsActive &&
            ticketAccess.GroupId == access.GroupId;

        if (!access.IsAdmin &&
            !isReporter &&
            !isAssignedUser &&
            !isGroupMember)
        {
            return NotFound();
        }

        var history = await _db.TicketStatusHistory
            .AsNoTracking()
            .Where(entry => entry.TicketId == id)
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .Select(entry => new TicketStatusHistoryResponse(
                entry.Id,
                entry.TicketId,
                entry.OldStatus,
                entry.NewStatus,
                entry.ChangedByUserId,
                entry.ChangedByUser.Name,
                entry.Reason,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(history);
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
            .Where(user =>
                user.Id == userId &&
                user.IsActive &&
                user.RegistrationStatus == RegistrationStatus.Approved)
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

    private async Task<UserAccess?> GetCurrentUserAccessAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                user.IsActive &&
                user.RegistrationStatus == RegistrationStatus.Approved)
            .Select(user => new UserAccess(
                user.IsAdmin,
                user.GroupId,
                user.Group != null && user.Group.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Ticket> GetTicketResponseQuery()
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

    private static string GetClosureTypeName(int closureTypeId)
    {
        return (TicketClosureType)closureTypeId switch
        {
            TicketClosureType.Megoldva =>
                "Megoldva",

            TicketClosureType.MegoldvaKerulomegoldassal =>
                "Megoldva kerülőmegoldással",

            TicketClosureType.NemReprodukalhato =>
                "Nem reprodukálható",

            TicketClosureType.Duplikalt =>
                "Duplikált",

            TicketClosureType.NemJavithato =>
                "Nem javítható",

            _ => "Ismeretlen lezárási típus"
        };
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

    private sealed record UserAccess(
        bool IsAdmin,
        int? GroupId,
        bool GroupIsActive);
}