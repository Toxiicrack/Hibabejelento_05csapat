using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveCategories(
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}