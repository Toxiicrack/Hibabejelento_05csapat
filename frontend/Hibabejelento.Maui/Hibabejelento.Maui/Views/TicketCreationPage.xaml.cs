using Hibabejelento.Maui.ViewModels;

namespace Hibabejelento.Maui.Views;

public partial class TicketCreationPage : ContentPage
{
	private TicketCreationViewModel _vm;
	public TicketCreationPage(TicketCreationViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = _vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadCategoriesCommand.ExecuteAsync(null);
	}
}