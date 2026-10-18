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
[Route("api/admin/categories")]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var categories = await _db.Categories
            .AsNoTracking()
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

    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var name = request.Name.Trim();

        var alreadyExists = await _db.Categories.AnyAsync(
            category => category.Name.ToLower() == name.ToLower(),
            cancellationToken);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = "Már létezik ilyen nevű kategória."
            });
        }

        var category = new Category
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            "/api/categories",
            new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt));
    }

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await CurrentUserIsAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var category = await _db.Categories
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = false;
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