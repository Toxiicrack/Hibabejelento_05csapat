using System.ComponentModel.DataAnnotations;
using Hibabejelento.Api.Middleware.Enums;

namespace Hibabejelento.Api.DTOs;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserResponse(
    int Id,
    string Name,
    string Email,
    bool IsAdmin,
    int? GroupId,
    string? GroupName,
    bool IsActive,
    RegistrationStatus RegistrationStatus,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? LastLoginAt);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);

public sealed class AssignGroupRequest
{
    // Null értékkel eltávolítható a felhasználó a csoportból.
    public int? GroupId { get; init; }
}

public sealed class CreateGroupRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class UpdateGroupRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}