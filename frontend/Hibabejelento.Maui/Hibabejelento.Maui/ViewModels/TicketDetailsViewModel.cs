using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;

namespace Hibabejelento.Maui.ViewModels;

[QueryProperty(nameof(SelectedTicket), "SelectedTicket")]
public partial class TicketDetailsViewModel : BaseViewModel
{
    private readonly IApiService _api;
    private int _currentUserId;
    private bool _isAdmin;
    private bool _isMaintenanceMember;
    private bool _isInitializing;
    private int _loadedTicketId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanWithdraw))]
    [NotifyPropertyChangedFor(nameof(CanReopen))]
    [NotifyPropertyChangedFor(nameof(CanOpenMaintenance))]
    [NotifyPropertyChangedFor(nameof(CanAddNote))]
    [NotifyPropertyChangedFor(nameof(ReporterDisplayName))]
    private Ticket? selectedTicket;

    [ObservableProperty] private ObservableCollection<TicketNote> notes = new();
    [ObservableProperty] private ObservableCollection<TicketStatusHistory> statusHistory = new();
    [ObservableProperty] private ObservableCollection<Notification> ticketNotifications = new();
    [ObservableProperty] private string newNote = string.Empty;

    public string ReporterDisplayName =>
        !string.IsNullOrWhiteSpace(SelectedTicket?.ReporterName)
            ? SelectedTicket!.ReporterName
            : (SelectedTicket != null ? $"#USER-{SelectedTicket.ReporterId}" : string.Empty);

    public bool CanWithdraw =>
        SelectedTicket != null &&
        SelectedTicket.Status != "Visszavonva" &&
        SelectedTicket.Status != "Lezárt" &&
        (_currentUserId == 0 || SelectedTicket.ReporterId == _currentUserId);

    public bool CanReopen =>
        SelectedTicket?.Status == "Lezárt" &&
        (_currentUserId == 0 || SelectedTicket.ReporterId == _currentUserId);

    public bool CanOpenMaintenance =>
        SelectedTicket != null && (_isMaintenanceMember || _isAdmin);

    public bool CanAddNote =>
        SelectedTicket != null &&
        SelectedTicket.Status != "Lezárt" &&
        SelectedTicket.Status != "Visszavonva";

    public TicketDetailsViewModel(IApiService api) => _api = api;

    partial void OnSelectedTicketChanged(Ticket? value)
    {
        if (value == null || _isInitializing) return;

        if (_loadedTicketId == value.Id && (Notes.Count > 0 || StatusHistory.Count > 0))
            return;

        _ = InitializeTicketAsync(value);
    }

    private async Task InitializeTicketAsync(Ticket ticket)
    {
        if (_isInitializing) return;
        _isInitializing = true;

        try
        {
            var uidStr = await SecureStorage.GetAsync("userId");
            int.TryParse(uidStr, out _currentUserId);

            var isAdminStr = await SecureStorage.GetAsync("userIsAdmin");
            bool.TryParse(isAdminStr, out _isAdmin);

            var gidStr = await SecureStorage.GetAsync("userGroupId");
            _isMaintenanceMember = int.TryParse(gidStr, out var gid) && gid > 0;

            RefreshComputedProperties();

            if (!string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                var fresh = await _api.GetTicketAsync(ticket.TicketNumber);
                if (fresh != null)
                {
                    selectedTicket = fresh;
                    RefreshComputedProperties();
                }
            }

            _loadedTicketId = selectedTicket?.Id ?? ticket.Id;
            await LoadNotesAsync();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(SelectedTicket));
        OnPropertyChanged(nameof(ReporterDisplayName));
        OnPropertyChanged(nameof(CanWithdraw));
        OnPropertyChanged(nameof(CanReopen));
        OnPropertyChanged(nameof(CanOpenMaintenance));
        OnPropertyChanged(nameof(CanAddNote));
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (SelectedTicket == null) return;

        var fetchedNotes = await _api.GetTicketNotesAsync(SelectedTicket.Id);
        var fetchedHistory = await _api.GetTicketStatusHistoryAsync(SelectedTicket.Id);
        var fetchedNotifications = (await _api.GetNotificationsAsync(false, 100))
            .Where(x => x.TicketId == SelectedTicket.Id)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Notes.Clear();
            foreach (var n in fetchedNotes)
                Notes.Add(n);

            StatusHistory.Clear();
            foreach (var h in fetchedHistory)
                StatusHistory.Add(h);

            TicketNotifications.Clear();
            foreach (var notif in fetchedNotifications)
                TicketNotifications.Add(notif);
        });
    }

    [RelayCommand]
    public async Task AddNoteAsync()
    {
        if (SelectedTicket == null || string.IsNullOrWhiteSpace(NewNote)) return;
        if (await _api.AddTicketNoteAsync(SelectedTicket.Id, NewNote.Trim()))
        {
            NewNote = string.Empty;
            await RefreshTicketAndHistoryAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A bejegyzés elküldése nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task ReopenTicketAsync()
    {
        if (SelectedTicket == null || !CanReopen) return;
        var reason = await Shell.Current.DisplayPromptAsync(
            "Hibajegy újranyitása",
            "Miért nem vagy elégedett a szolgáltatással?",
            "Újranyitás",
            "Mégse",
            "Indoklás",
            maxLength: 5000);
        if (string.IsNullOrWhiteSpace(reason)) return;

        if (await _api.ReopenTicketAsync(SelectedTicket.Id, reason.Trim()))
        {
            await RefreshTicketAndHistoryAsync();
            await Shell.Current.DisplayAlert("Sikeres", "A hibajegy újra lett nyitva.", "Rendben");
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az újranyitás nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task OpenMaintenanceAsync()
    {
        if (SelectedTicket == null || !CanOpenMaintenance) return;
        await Shell.Current.GoToAsync(
            nameof(Hibabejelento.Maui.Views.TicketEditPage),
            new Dictionary<string, object> { ["SelectedTicket"] = SelectedTicket });
    }

    [RelayCommand]
    public async Task Close() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    public async Task WithdrawTicket()
    {
        if (SelectedTicket == null || !CanWithdraw) return;
        bool confirm = await Shell.Current.DisplayAlert(
            "Megerősítés",
            $"Biztosan vissza szeretnéd vonni a(z) {SelectedTicket.TicketNumber} hibajegyet?",
            "Igen",
            "Mégse");
        if (!confirm) return;

        if (await _api.WithdrawTicketAsync(SelectedTicket.Id))
        {
            await RefreshTicketAndHistoryAsync();
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A visszavonás nem sikerült.", "Rendben");
        }
    }

    private async Task RefreshTicketAndHistoryAsync()
    {
        if (SelectedTicket != null && !string.IsNullOrWhiteSpace(SelectedTicket.TicketNumber))
        {
            var fresh = await _api.GetTicketAsync(SelectedTicket.TicketNumber);
            if (fresh != null)
            {
                selectedTicket = fresh;
            }
        }

        RefreshComputedProperties();
        await LoadNotesAsync();
    }
}