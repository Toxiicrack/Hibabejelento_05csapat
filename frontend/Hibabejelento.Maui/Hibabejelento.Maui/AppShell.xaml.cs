using Hibabejelento.Maui.Views;

namespace Hibabejelento.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(TicketCreationPage), typeof(TicketCreationPage));
            Routing.RegisterRoute(nameof(TicketDetailsPage), typeof(TicketDetailsPage));
            Routing.RegisterRoute(nameof(TicketEditPage), typeof(TicketEditPage));
            Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
            Routing.RegisterRoute(nameof(FilterPage), typeof(FilterPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));            
        }
    }
}
