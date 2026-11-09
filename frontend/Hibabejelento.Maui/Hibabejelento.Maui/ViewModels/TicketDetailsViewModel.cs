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
    [QueryProperty(nameof(SelectedTicket), "SelectedTicket")]
    public partial class TicketDetailsViewModel: BaseViewModel
    {        
        private readonly IApiService _api;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanWithdraw))]
        private Ticket? selectedTicket;

        public bool CanWithdraw =>
            SelectedTicket != null &&
            SelectedTicket.Status != "Visszavonva" &&
            SelectedTicket.Status != "Lezárt";

        public TicketDetailsViewModel(IApiService api)
        {
            _api = api;
        }

        [RelayCommand]
        public async Task Close()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task WithdrawTicket()
        {
            if (SelectedTicket is null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Megerősítés",
                $"Biztosan vissza szeretnéd vonni a(z) {SelectedTicket.TicketNumber} azonosítójú hibajegyet?",
                "Igen, visszavonom",
                "Mégse");

            if (!confirm) return;

            bool success = await _api.WithdrawTicketAsync(SelectedTicket.Id);
            if (success)
            {
                SelectedTicket.Status = "Visszavonva";
                OnPropertyChanged(nameof(SelectedTicket));
                OnPropertyChanged(nameof(CanWithdraw));

                await Shell.Current.DisplayAlert("Sikeres", "A hibajegy sikeresen visszavonva.", "Rendben");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Hiba", "A visszavonás nem sikerült. Csak saját, aktív jegy vonható vissza.", "Rendben");
            }
        }
    }
}
