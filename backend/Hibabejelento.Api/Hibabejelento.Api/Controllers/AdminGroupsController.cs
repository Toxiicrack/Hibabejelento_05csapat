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
[Route("api/admin/groups")]
public sealed class AdminGroupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminGroupsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups(
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var groups = await _db.Groups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .Select(group => new
            {
                group.Id,
                group.Name,
                group.Description,
                group.IsActive,
                group.CreatedAt,
                MemberCount = group.Users.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup(
        CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        var duplicateName = await _db.Groups.AnyAsync(
            group => group.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (duplicateName)
        {
            return Conflict(new
            {
                message = "Már létezik ilyen nevű csoport."
            });
        }

        var group = new Group
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetGroups),
            new { id = group.Id },
            new
            {
                group.Id,
                group.Name,
                group.Description,
                group.IsActive,
                group.CreatedAt
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateGroup(
        int id,
        UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var group = await _db.Groups
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();

        var duplicateName = await _db.Groups.AnyAsync(
            candidate =>
                candidate.Id != id &&
                candidate.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (duplicateName)
        {
            return Conflict(new
            {
                message = "Már létezik ilyen nevű csoport."
            });
        }

        group.Name = name;
        group.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateGroup(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var group = await _db.Groups
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var hasActiveMembers = await _db.Users.AnyAsync(
            user => user.GroupId == id && user.IsActive,
            cancellationToken);

        if (hasActiveMembers)
        {
            return Conflict(new
            {
                message = "A csoport inaktiválásához előbb rendeld át vagy inaktiváld az aktív tagokat."
            });
        }

        group.IsActive = false;
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