using CommunityToolkit.Mvvm.ComponentModel;
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
        
        [ObservableProperty] private Ticket? selectedTicket;

        public TicketDetailsViewModel(IApiService api)
        {
            _api = api;
        }
    }
}
