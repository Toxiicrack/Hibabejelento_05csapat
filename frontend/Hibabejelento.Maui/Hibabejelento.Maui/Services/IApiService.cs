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

    // Admin
    Task<IEnumerable<Ticket>> GetAdminTicketsAsync(bool newOnly = false);
    Task<bool> AcceptTicketAsync(int ticketId, AcceptTicketRequest request);

    // Admin - Users
    Task<List<UserDto>?> GetAdminUsersAsync(bool pendingOnly = false);
    Task<bool> CreateUserAsync(string name, string email, string password, string roleType, int? groupId);
    Task<bool> ApproveUserAsync(int userId);
    Task<bool> DeactivateUserAsync(int userId);
    Task<bool> AssignUserGroupAsync(int userId, int? groupId);

    // Admin - Groups
    Task<List<Group>?> GetAdminGroupsAsync();
    Task<bool> CreateGroupAsync(string name, string? description);
    Task<bool> UpdateGroupAsync(int id, string name, string? description);
    Task<bool> DeactivateGroupAsync(int id);

    // Admin - Categories
    Task<List<Category>?> GetAdminCategoriesAsync();
    Task<bool> CreateCategoryAsync(string name, string? description);
    Task<bool> DeactivateCategoryAsync(int id);

    // Admin - Locations
    Task<List<Models.Location>?> GetAdminLocationsAsync();
    Task<bool> CreateLocationAsync(string name, string? description);
    Task<bool> UpdateLocationAsync(int id, string name, string? description);
    Task<bool> DeactivateLocationAsync(int id);

    // Admin - Priorities
    Task<List<Priority>?> GetAdminPrioritiesAsync();
    Task<bool> CreatePriorityAsync(string name, int defaultDueDays, bool isDefault);
    Task<bool> UpdatePriorityAsync(int id, string name, int defaultDueDays, bool isDefault);
    Task<bool> DeactivatePriorityAsync(int id);
}
