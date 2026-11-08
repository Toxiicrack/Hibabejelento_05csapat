using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class TicketDetailsPage : ContentPage
{
	public TicketDetailsPage(TicketDetailsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}