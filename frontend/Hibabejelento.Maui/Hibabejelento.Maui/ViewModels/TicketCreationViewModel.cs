using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;

namespace Hibabejelento.Maui.ViewModels
{
    public partial class TicketCreationViewModel : BaseViewModel
    {
        private readonly IApiService _api;
        [ObservableProperty] public string ticketTitle = string.Empty;
        [ObservableProperty] public string ticketDescription = string.Empty;
        [ObservableProperty] public string ticketAssetIdentifier = string.Empty;
        [ObservableProperty] public string ticketError = string.Empty;

        public ObservableCollection<Category> Categories { get; } = new();
        [ObservableProperty] public Category? selectedCategory;

        public ObservableCollection<Models.Location> Locations { get; } = new();
        [ObservableProperty] public Models.Location? selectedLocation;

        public TicketCreationViewModel(IApiService api)
        {
            _api = api;
        }

        [RelayCommand]
        public async Task LoadFormDataAsync()
        {
            await Task.WhenAll(LoadCategoriesAsync(), LoadLocationsAsync());
        }

        [RelayCommand]
        public async Task LoadCategoriesAsync()
        {
            var list = await _api.GetCategoriesAsync();
            Categories.Clear();
            if (list is not null)
            {
                foreach (var cat in list)
                {
                    Categories.Add(cat);
                }
                if (Categories.Count > 0 && SelectedCategory is null)
                {
                    SelectedCategory = Categories[0];
                }
            }
        }

        [RelayCommand]
        public async Task LoadLocationsAsync()
        {
            var list = await _api.GetLocationsAsync();
            Locations.Clear();
            if (list is not null)
            {
                foreach (var loc in list)
                {
                    Locations.Add(loc);
                }
                if (Locations.Count > 0 && SelectedLocation is null)
                {
                    SelectedLocation = Locations[0];
                }
            }
        }

        [RelayCommand]
        public async Task CreateTicket()
        {
            var token = await SecureStorage.GetAsync("userToken");
            if (string.IsNullOrEmpty(token))
            {
                TicketError = "Kérem jelentkezzen be a ticket létrehozásához!";
                return;
            }
            TicketError = string.Empty;

            if (string.IsNullOrWhiteSpace(TicketTitle) ||
                TicketTitle.Trim().Length < 3 ||
                TicketTitle.Trim().Length > 200)
            {
                TicketError = "Nem megfelelő hibajegy cím (3 - 200 karakter)!";
                return;
            }

            if (SelectedCategory is null)
            {
                TicketError = "Kérjük, válasszon egy kategóriát!";
                return;
            }

            if (SelectedLocation is null)
            {
                TicketError = "Kérjük, válasszon egy helyszínt!";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketDescription) ||
                TicketDescription.Trim().Length < 10)
            {
                TicketError = "Nem megfelelő hibajegy leírás (legalább 10 karakter)!";
                return;
            }

            if (TicketAssetIdentifier?.Length > 100)
            {
                TicketError = "Nem megfelelő eszközazonosító (max 100 karakter)!";
                return;
            }

            bool success = await _api.CreateTicket(new Models.TicketCreation
            {
                Title = TicketTitle.Trim(),
                AssetIdentifier = string.IsNullOrWhiteSpace(TicketAssetIdentifier) ? null : TicketAssetIdentifier.Trim(),
                CategoryId = SelectedCategory.Id,
                Description = TicketDescription.Trim(),
                LocationId = SelectedLocation.Id
            });
            if (success)
            {
                await Shell.Current.DisplayAlert("Sikeres", "A hibajegy bejelentése sikeresen megtörtént!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                TicketError = "Sikertelen hibajegy-létrehozás. Ellenőrizze a kapcsolatot és próbálja újra.";
            }
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
