using Hibabejelento.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        bool dbOk;
        try
        {
            dbOk = await _db.Database.CanConnectAsync();
        }
        catch
        {
            dbOk = false;
        }

        return Ok(new
        {
            status = dbOk ? "Healthy" : "Degraded",
            database = dbOk ? "Connected" : "Unavailable",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}