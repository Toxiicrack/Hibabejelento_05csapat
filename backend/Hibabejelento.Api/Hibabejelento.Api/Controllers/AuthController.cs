using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hibabejelento.Api.Data;
using Hibabejelento.Api.DTOs;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Middleware.Enums;
using Hibabejelento.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notifications;

    public AuthController(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        INotificationService notifications)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _notifications = notifications;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var emailAlreadyExists = await _db.Users
            .AnyAsync(user => user.Email == email, cancellationToken);

        if (emailAlreadyExists)
        {
            return Conflict(new
            {
                message = "Ezzel az e-mail-címmel már létezik felhasználó."
            });
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            IsAdmin = false,
            GroupId = null,
            IsActive = true,
            RegistrationStatus = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAdminsAsync(
            ticketId: null,
            NotificationType.UjRegisztracio,
            $"Új regisztráció vár jóváhagyásra: {user.Name} ({user.Email}).",
            excludedUserId: null,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Accepted(new
        {
            user.Id,
            user.RegistrationStatus,
            message = "A regisztráció adminisztrátori jóváhagyásra vár."
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _db.Users
            .Include(candidate => candidate.Group)
            .SingleOrDefaultAsync(
                candidate => candidate.Email == email,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.RegistrationStatus != RegistrationStatus.Approved)
        {
            return Unauthorized(new
            {
                message = "A megadott adatokkal nem lehet bejelentkezni."
            });
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "A megadott adatokkal nem lehet bejelentkezni."
            });
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresAtUtc) = _tokenService.CreateToken(user);

        return Ok(new LoginResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresAtUtc: expiresAtUtc,
            User: ToResponse(user)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(candidate => candidate.Group)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(ToResponse(user));
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            IsAdmin: user.IsAdmin,
            GroupId: user.GroupId,
            GroupName: user.Group?.Name,
            IsActive: user.IsActive,
            RegistrationStatus: user.RegistrationStatus,
            CreatedAt: user.CreatedAt,
            ApprovedAt: user.ApprovedAt,
            LastLoginAt: user.LastLoginAt);
    }
}