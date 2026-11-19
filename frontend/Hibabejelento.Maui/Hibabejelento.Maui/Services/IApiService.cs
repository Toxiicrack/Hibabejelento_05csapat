using Hibabejelento.Maui.Models;

namespace Hibabejelento.Maui.Services;

public interface IApiService
{
    Task<HealthStatus?> GetHealthAsync();
    Task<bool> CreateTicket(TicketCreation newTicket);
    Task<Ticket?> GetTicketAsync(string ticketNumber);
    Task<IEnumerable<Ticket>> GetMyTicketsAsync();
    Task<IEnumerable<Ticket>> GetMaintenanceTicketsAsync();
    Task<string> Login(string email, string password);
    Task<List<Category>?> GetCategoriesAsync();
    Task<List<Models.Location>?> GetLocationsAsync();
    Task<bool> WithdrawTicketAsync(int ticketId);
    Task<List<TicketNote>> GetTicketNotesAsync(int ticketId);
    Task<bool> AddTicketNoteAsync(int ticketId, string message);
    Task<bool> ReopenTicketAsync(int ticketId, string reason);
    Task<bool> TakeTicketAsync(int ticketId);
    Task<bool> CloseTicketAsync(int ticketId, int closureTypeId, string resolution);
    Task<List<TicketStatusHistory>> GetTicketStatusHistoryAsync(int ticketId);
    Task<List<Notification>> GetNotificationsAsync(bool unreadOnly = false, int take = 50);
    Task<int> GetUnreadNotificationCountAsync();
    Task<bool> MarkNotificationReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsReadAsync();
}
