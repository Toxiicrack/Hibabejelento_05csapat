using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/priorities")]
public sealed class PrioritiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PrioritiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetActivePriorities(
        CancellationToken cancellationToken)
    {
        var priorities = await _db.Priorities
            .AsNoTracking()
            .Where(priority => priority.IsActive)
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
}