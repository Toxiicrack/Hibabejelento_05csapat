using Hibabejelento.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) SQLite DbContext
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? "Data Source=hibabejelento.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(conn));

// 2) CORS – hogy a MAUI elérhesse
builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// 3) Controllerek
builder.Services.AddControllers();

// 4) Beépített OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// 5) DB létrehozás, ha nincs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// 6) OpenAPI dokumentum és Swagger UI development módban
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Hibabejelentő API v1");
        options.RoutePrefix = "swagger"; // http://localhost:5000/swagger
    });
}

app.UseCors("MauiPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();