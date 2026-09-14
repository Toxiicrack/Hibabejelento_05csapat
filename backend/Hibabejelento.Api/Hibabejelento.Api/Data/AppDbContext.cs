using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

}