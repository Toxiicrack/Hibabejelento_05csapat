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
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminUsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var query = _db.Users
            .AsNoTracking()
            .Include(user => user.Group)
            .AsQueryable();

        if (pendingOnly)
        {
            query = query.Where(
                user => user.RegistrationStatus == RegistrationStatus.Pending);
        }

        var users = await query
            .OrderBy(user => user.CreatedAt)
            .Select(user => new UserResponse(
                user.Id,
                user.Name,
                user.Email,
                user.IsAdmin,
                user.GroupId,
                user.Group == null ? null : user.Group.Name,
                user.IsActive,
                user.RegistrationStatus,
                user.CreatedAt,
                user.ApprovedAt,
                user.LastLoginAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _db.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.RegistrationStatus == RegistrationStatus.Approved)
        {
            return Conflict(new
            {
                message = "A felhasználó regisztrációja már jóvá van hagyva."
            });
        }

        user.RegistrationStatus = RegistrationStatus.Approved;
        user.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId) ||
            !await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _db.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsActive)
        {
            return NoContent();
        }

        if (user.IsAdmin)
        {
            var activeAdminCount = await _db.Users.CountAsync(
                candidate =>
                    candidate.IsAdmin &&
                    candidate.IsActive &&
                    candidate.RegistrationStatus == RegistrationStatus.Approved,
                cancellationToken);

            if (activeAdminCount <= 1)
            {
                return Conflict(new
                {
                    message = "Az utolsó aktív adminisztrátor nem inaktiválható."
                });
            }
        }

        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:int}/group")]
    public async Task<IActionResult> AssignGroup(
        int id,
        AssignGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var user = await _db.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (request.GroupId is int groupId)
        {
            var groupExistsAndIsActive = await _db.Groups.AnyAsync(
                group => group.Id == groupId && group.IsActive,
                cancellationToken);

            if (!groupExistsAndIsActive)
            {
                return BadRequest(new
                {
                    message = "A megadott csoport nem létezik vagy inaktív."
                });
            }
        }

        user.GroupId = request.GroupId;
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
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