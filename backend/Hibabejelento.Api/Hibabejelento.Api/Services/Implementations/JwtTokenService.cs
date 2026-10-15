using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hibabejelento.Api.Middleware.Entities;
using Hibabejelento.Api.Options;
using Hibabejelento.Api.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Hibabejelento.Api.Services.Implementations;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.LifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

        return (serializedToken, expiresAtUtc);
    }
}