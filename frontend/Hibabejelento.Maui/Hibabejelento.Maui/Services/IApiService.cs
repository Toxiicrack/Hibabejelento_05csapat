using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public interface IApiService
{
    Task<HealthStatus?> GetHealthAsync();
    Task<bool> CreateTicket(TicketCreation newTicket);
    Task<Ticket?> GetTicketAsync(int ticketId);
    Task<IEnumerable<Ticket>> GetTicketsAsync();
    Task<string> Login(string email, string password);


}