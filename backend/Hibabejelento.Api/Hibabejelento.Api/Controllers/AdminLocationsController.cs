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
[Route("api/admin/locations")]
public sealed class AdminLocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminLocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations(
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .Select(location => new LocationResponse(
                location.Id,
                location.Name,
                location.Description,
                location.IsActive,
                location.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(locations);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLocation(
        SaveLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        if (await _db.Locations.AnyAsync(
                location => location.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Conflict(new { message = "Már létezik ilyen nevű helyszín." });
        }

        var location = new Location
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            "/api/locations",
            new LocationResponse(
                location.Id,
                location.Name,
                location.Description,
                location.IsActive,
                location.CreatedAt));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateLocation(
        int id,
        SaveLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var location = await _db.Locations
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (location is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();

        if (await _db.Locations.AnyAsync(
                candidate =>
                    candidate.Id != id &&
                    candidate.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            return Conflict(new { message = "Már létezik ilyen nevű helyszín." });
        }

        location.Name = name;
        location.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateLocation(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var location = await _db.Locations
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (location is null)
        {
            return NotFound();
        }

        location.IsActive = false;
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