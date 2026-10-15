using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Hibabejelento.Api.Data;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Middleware.Enums;
using Hibabejelento.Api.Options;
using Hibabejelento.Api.Services.Implementations;
using Hibabejelento.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Hibabejelento.Api;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=hibabejelento.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>();

if (jwtOptions is null ||
    string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
    Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32 ||
    jwtOptions.LifetimeMinutes <= 0)
{
    throw new InvalidOperationException(
        "A Jwt konfiguráció hiányos. Állítsd be a Jwt:Issuer, Jwt:Audience, " +
        "Jwt:SigningKey és Jwt:LifetimeMinutes értékeket User Secretsben.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiPolicy", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = JwtRegisteredClaimNames.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?
                    .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (!int.TryParse(subject, out var userId))
                {
                    context.Fail(
                        "A token nem tartalmaz érvényes felhasználóazonosítót.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                var accountIsUsable = await db.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id == userId &&
                            user.IsActive &&
                            user.RegistrationStatus ==
                                RegistrationStatus.Approved,
                        context.HttpContext.RequestAborted);

                if (!accountIsUsable)
                {
                    context.Fail(
                        "A felhasználói fiók nem aktív vagy nincs jóváhagyva.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Hibabejelentő API v1");

        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("MauiPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();