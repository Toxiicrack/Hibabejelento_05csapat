using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hibabejelento.Maui.ViewModels
{
    public partial class TicketCreationViewModel: BaseViewModel
    {
        private readonly IApiService _api;
        [ObservableProperty] public string ticketTitle = string.Empty;
        [ObservableProperty] public string ticketDescription = string.Empty;
        [ObservableProperty] public string ticketLocation = string.Empty;
        [ObservableProperty] public string ticketAssetIdentifier = string.Empty;
        [ObservableProperty] public string ticketError = string.Empty;

        public ObservableCollection<Category> Categories { get; } = new();
        [ObservableProperty] public Category? selectedCategory;

        public TicketCreationViewModel(IApiService api)
        {
            _api = api;
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

            if (string.IsNullOrWhiteSpace(TicketDescription) ||
                TicketDescription.Trim().Length < 10)
            {
                TicketError = "Nem megfelelő hibajegy leírás (legalább 10 karakter)!";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketLocation) ||
                TicketLocation.Trim().Length < 2 ||
                TicketLocation.Trim().Length > 300)
            {
                TicketError = "Nem megfelelő terület (2 - 300 karakter)!";
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
                Location = TicketLocation.Trim()
            });
            if (success){
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
