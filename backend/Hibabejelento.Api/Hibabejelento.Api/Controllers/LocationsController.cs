using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LocationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveLocations(
        CancellationToken cancellationToken)
    {
        var locations = await _db.Locations
            .AsNoTracking()
            .Where(location => location.IsActive)
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
}