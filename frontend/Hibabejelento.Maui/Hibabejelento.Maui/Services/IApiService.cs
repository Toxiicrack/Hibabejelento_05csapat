using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public interface IApiService
{
    Task<HealthStatus?> GetHealthAsync();
}