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
[Route("api/admin/priorities")]
public sealed class AdminPrioritiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminPrioritiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetPriorities(
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var priorities = await _db.Priorities
            .AsNoTracking()
            .OrderBy(priority => priority.DefaultDueDays)
            .Select(priority => new PriorityResponse(
                priority.Id,
                priority.Name,
                priority.DefaultDueDays,
                priority.IsDefault,
                priority.IsActive,
                priority.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(priorities);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePriority(
        SavePriorityRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        if (await _db.Priorities.AnyAsync(
                priority => priority.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Conflict(new { message = "Már létezik ilyen nevű prioritás." });
        }

        var hasActiveDefault = await _db.Priorities.AnyAsync(
            priority => priority.IsActive && priority.IsDefault,
            cancellationToken);

        if (!hasActiveDefault && !request.IsDefault)
        {
            return BadRequest(new
            {
                message = "Az első prioritásnak alapértelmezettnek kell lennie."
            });
        }

        if (request.IsDefault)
        {
            var existingDefaults = await _db.Priorities
                .Where(priority => priority.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        var priority = new Priority
        {
            Name = name,
            DefaultDueDays = request.DefaultDueDays,
            IsDefault = request.IsDefault || !hasActiveDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Priorities.Add(priority);
        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            "/api/priorities",
            new PriorityResponse(
                priority.Id,
                priority.Name,
                priority.DefaultDueDays,
                priority.IsDefault,
                priority.IsActive,
                priority.CreatedAt));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePriority(
        int id,
        SavePriorityRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var priority = await _db.Priorities
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (priority is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();

        if (await _db.Priorities.AnyAsync(
                candidate =>
                    candidate.Id != id &&
                    candidate.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Conflict(new { message = "Már létezik ilyen nevű prioritás." });
        }

        if (request.IsDefault && !priority.IsActive)
        {
            return BadRequest(new
            {
                message = "Inaktív prioritás nem lehet alapértelmezett."
            });
        }

        if (!request.IsDefault &&
            priority.IsDefault &&
            !await _db.Priorities.AnyAsync(
                candidate =>
                    candidate.Id != id &&
                    candidate.IsActive &&
                    candidate.IsDefault,
                cancellationToken))
        {
            return Conflict(new
            {
                message = "Legalább egy aktív alapértelmezett prioritásnak lennie kell."
            });
        }

        if (request.IsDefault)
        {
            var existingDefaults = await _db.Priorities
                .Where(candidate =>
                    candidate.Id != id &&
                    candidate.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        priority.Name = name;
        priority.DefaultDueDays = request.DefaultDueDays;
        priority.IsDefault = request.IsDefault;

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivatePriority(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var priority = await _db.Priorities
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (priority is null)
        {
            return NotFound();
        }

        if (priority.IsDefault)
        {
            return Conflict(new
            {
                message = "Az alapértelmezett prioritást előbb cseréld le."
            });
        }

        priority.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<bool> CurrentUserIsAdminAsync(
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(subject, out var userId))
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
}