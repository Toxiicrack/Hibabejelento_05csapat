using CommunityToolkit.Mvvm.ComponentModel;

namespace Hibabejelento.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
}