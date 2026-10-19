using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        [ObservableProperty] public string ticketTitle;
        [ObservableProperty] public string ticketDescription;
        [ObservableProperty] public string ticketLocation;
        [ObservableProperty] public string ticketAssetIdentifier;
        [ObservableProperty] public int ticketCategoryId = 1;
        [ObservableProperty] public string ticketError = string.Empty;
        [ObservableProperty] public Dictionary<int, string> ticketcategories = new();
        public TicketCreationViewModel(IApiService api)
        {
            _api = api;
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
                TicketTitle.Length < 3 ||
                TicketTitle.Length > 200)
            {
                TicketError = "Nem megfelelő hibajegy cím!";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketDescription) ||
                TicketDescription.Length <= 10)
            {
                TicketError = "Nem megfelelő hibajegy leírás!";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketLocation) ||
                TicketLocation.Length < 2 ||
                TicketLocation.Length > 300)
            {
                TicketError = "Nem megfelelő terület!";
                return;
            }
            
            if (TicketAssetIdentifier?.Length > 100)
            {
                TicketError = "Nem megfelelő eszközazonosító!";
                return;
            }
            
            bool success = await _api.CreateTicket(new Models.TicketCreation
            {
                Title = TicketTitle,
                AssetIdentifier = TicketAssetIdentifier,
                CategoryId = TicketCategoryId,
                Description = TicketDescription,
                Location = TicketLocation
            });
            if (success){
                await Shell.Current.GoToAsync(nameof(MainPage));
            }
            else
            {
                ticketError = "sikertelen létrehozás..";
            }
        }
    }
}
