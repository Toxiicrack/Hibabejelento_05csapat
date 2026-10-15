namespace Hibabejelento.Api.Options;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 30;
}