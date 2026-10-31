using System.IdentityModel.Tokens.Jwt;

namespace Hibabejelento.Services;

/// <summary>
/// Kliensoldali JWT-segédfüggvények. A token aláírását ez a dekódolás
/// önmagában nem ellenőrzi; hitelesítésre csak a backend használja.
/// </summary>
public static class JWTTokenService
{
    public static JwtSecurityToken ConvertJwtStringToJwtSecurityToken(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            throw new ArgumentException("A JWT token nem lehet üres.", nameof(jwt));

        return new JwtSecurityTokenHandler().ReadJwtToken(jwt);
    }

    public static List<string> DecodeJwt(JwtSecurityToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Claims.Select(claim => claim.Value).ToList();
    }
}
