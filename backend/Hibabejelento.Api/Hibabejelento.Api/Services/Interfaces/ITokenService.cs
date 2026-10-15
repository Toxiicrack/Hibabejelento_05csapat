using Hibabejelento.Api.Middleware.Entities;

namespace Hibabejelento.Api.Services.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateToken(User user);
}