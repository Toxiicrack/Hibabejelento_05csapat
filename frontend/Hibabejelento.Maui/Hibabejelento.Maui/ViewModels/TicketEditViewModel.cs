using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;

namespace Hibabejelento.Maui.ViewModels;

[QueryProperty(nameof(SelectedTicket), "SelectedTicket")]
public partial class TicketEditViewModel : BaseViewModel
{
    private readonly IApiService _api;
    private int _currentUserId;
    private bool _isCurrentUserAdmin;
    private int? _currentUserGroupId;
    private bool _isInitializing;
    private int _loadedTicketId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTakeTicket))]
    [NotifyPropertyChangedFor(nameof(CanCloseTicket))]
    [NotifyPropertyChangedFor(nameof(CanAddNote))]
    [NotifyPropertyChangedFor(nameof(CanEditTicket))]
    private Ticket? selectedTicket;

    [ObservableProperty] private ObservableCollection<TicketNote> notes = new();
    [ObservableProperty] private ObservableCollection<TicketStatusHistory> statusHistory = new();
    [ObservableProperty]
    private ObservableCollection<string> priorities = new()
    {
        "Alacsony",
        "Normál",
        "Magas",
        "Sürgős",
        "Kritikus"
    };
    [ObservableProperty] private List<Category> categories = new();
    [ObservableProperty] private List<Models.Location> locations = new();

    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private Models.Location? selectedLocation;
    [ObservableProperty] private string selectedPriority = "Normál";
    [ObservableProperty] private string editTitle = string.Empty;
    [ObservableProperty] private string editDescription = string.Empty;
    [ObservableProperty] private string assigneeUserIdInput = string.Empty;

    [ObservableProperty] private string newNote = string.Empty;
    [ObservableProperty] private string resolution = string.Empty;
    [ObservableProperty] private int closureTypeId = 1;

    public bool IsCurrentUserAdmin => _isCurrentUserAdmin;

    private bool IsMaintenanceOrAdmin =>
        _isCurrentUserAdmin ||
        _currentUserGroupId.HasValue ||
        (SelectedTicket != null && _currentUserId != 0 && SelectedTicket.AssignedUserId == _currentUserId);

    public bool CanTakeTicket =>
        SelectedTicket != null &&
        SelectedTicket.IsActive &&
        SelectedTicket.Status != "Lezárt" &&
        SelectedTicket.Status != "Visszavonva" &&
        SelectedTicket.AssignedUserId == null &&
        IsMaintenanceOrAdmin;

    public bool CanEditTicket =>
        SelectedTicket != null &&
        SelectedTicket.IsActive &&
        SelectedTicket.Status != "Lezárt" &&
        SelectedTicket.Status != "Visszavonva" &&
        IsMaintenanceOrAdmin;

    public bool CanCloseTicket =>
        SelectedTicket != null &&
        SelectedTicket.IsActive &&
        (SelectedTicket.Status == "Folyamatban" ||
         SelectedTicket.Status == "VálaszraVár" ||
         SelectedTicket.Status == "Újranyitva") &&
        (_isCurrentUserAdmin ||
         _currentUserId == 0 ||
         SelectedTicket.AssignedUserId == _currentUserId);

    public bool CanAddNote =>
        SelectedTicket != null &&
        SelectedTicket.Status != "Lezárt" &&
        SelectedTicket.Status != "Visszavonva" &&
        (IsMaintenanceOrAdmin ||
         _currentUserId == 0 ||
         SelectedTicket.ReporterId == _currentUserId);

    public List<string> ClosureTypes { get; } = new()
    {
        "Megoldva",
        "Megoldva kerülő megoldással",
        "Nem reprodukálható",
        "Duplikált",
        "Nem javítható"
    };

    public int SelectedClosureTypeIndex
    {
        get => ClosureTypeId - 1;
        set => ClosureTypeId = value + 1;
    }

    public TicketEditViewModel(IApiService api) => _api = api;

    partial void OnSelectedTicketChanged(Ticket? value)
    {
        if (value == null || _isInitializing) return;

        if (_loadedTicketId == value.Id && StatusHistory.Count > 0)
            return;

        PopulateFormFromTicket(value);
        _ = InitializeAsync(value);
    }

    private void PopulateFormFromTicket(Ticket ticket)
    {
        EditTitle = ticket.Title;
        EditDescription = ticket.Description;

        var priorityValue = string.IsNullOrWhiteSpace(ticket.Priority) ? "Normál" : ticket.Priority;
        if (!Priorities.Contains(priorityValue))
            Priorities.Add(priorityValue);
        SelectedPriority = priorityValue;

        AssigneeUserIdInput = ticket.AssignedUserId?.ToString() ?? string.Empty;

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == ticket.CategoryId);
        SelectedLocation = Locations.FirstOrDefault(l => l.Id == ticket.LocationId);
    }

    private async Task InitializeAsync(Ticket ticket)
    {
        if (_isInitializing) return;
        _isInitializing = true;

        try
        {
            var uidStr = await SecureStorage.GetAsync("userId");
            int.TryParse(uidStr, out _currentUserId);

            var isAdminStr = await SecureStorage.GetAsync("userIsAdmin");
            bool.TryParse(isAdminStr, out _isCurrentUserAdmin);

            var groupIdStr = await SecureStorage.GetAsync("userGroupId");
            _currentUserGroupId = int.TryParse(groupIdStr, out var gid) ? gid : null;

            OnPropertyChanged(nameof(IsCurrentUserAdmin));
            RefreshPermissionStates();

            if (Categories.Count == 0)
            {
                var loadedCategories = await _api.GetCategoriesAsync();
                if (loadedCategories != null)
                {
                    Categories = loadedCategories;
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == ticket.CategoryId);
                }
            }

            if (Locations.Count == 0)
            {
                var loadedLocations = await _api.GetLocationsAsync();
                if (loadedLocations != null)
                {
                    Locations = loadedLocations;
                    SelectedLocation = Locations.FirstOrDefault(l => l.Id == ticket.LocationId);
                }
            }

            if (!string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                var fresh = await _api.GetTicketAsync(ticket.TicketNumber);
                if (fresh != null)
                {
                    // Update backing field directly so OnSelectedTicketChanged is not re-triggered
                    selectedTicket = fresh;
                    OnPropertyChanged(nameof(SelectedTicket));
                    RefreshPermissionStates();
                    PopulateFormFromTicket(fresh);
                }
            }
            else
            {
                PopulateFormFromTicket(ticket);
            }

            _loadedTicketId = selectedTicket?.Id ?? ticket.Id;
            await LoadNotesAsync();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void RefreshPermissionStates()
    {
        OnPropertyChanged(nameof(CanTakeTicket));
        OnPropertyChanged(nameof(CanEditTicket));
        OnPropertyChanged(nameof(CanCloseTicket));
        OnPropertyChanged(nameof(CanAddNote));
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (SelectedTicket == null) return;

        var fetchedNotes = await _api.GetTicketNotesAsync(SelectedTicket.Id);
        var fetchedHistory = await _api.GetTicketStatusHistoryAsync(SelectedTicket.Id);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Notes.Clear();
            foreach (var note in fetchedNotes)
                Notes.Add(note);

            StatusHistory.Clear();
            foreach (var item in fetchedHistory)
                StatusHistory.Add(item);
        });
    }

    [RelayCommand]
    public async Task TakeTicketAsync()
    {
        if (SelectedTicket == null) return;
        if (await _api.TakeTicketAsync(SelectedTicket.Id))
        {
            await RefreshTicketAsync();
            await Shell.Current.DisplayAlert("Sikeres", "A hibajegyet átvetted.", "Rendben");
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A hibajegy átvétele nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task SaveTicketEditsAsync()
    {
        if (SelectedTicket == null) return;

        if (string.IsNullOrWhiteSpace(EditTitle) || EditTitle.Trim().Length < 3)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A cím legalább 3 karakter legyen.", "Rendben");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditDescription) || EditDescription.Trim().Length < 5)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A leírás nem lehet üres.", "Rendben");
            return;
        }

        SelectedTicket.Title = EditTitle.Trim();
        SelectedTicket.Description = EditDescription.Trim();
        SelectedTicket.Priority = SelectedPriority;

        if (SelectedCategory != null)
        {
            SelectedTicket.CategoryId = SelectedCategory.Id;
            SelectedTicket.CategoryName = SelectedCategory.Name;
        }

        if (SelectedLocation != null)
        {
            SelectedTicket.LocationId = SelectedLocation.Id;
            SelectedTicket.Location = SelectedLocation.Name;
        }

        if (!string.IsNullOrWhiteSpace(AssigneeUserIdInput))
        {
            if (int.TryParse(AssigneeUserIdInput.Trim(), out var assigneeId) && assigneeId > 0)
            {
                SelectedTicket.AssignedUserId = assigneeId;
            }
            else
            {
                await Shell.Current.DisplayAlert("Érvénytelen ID", "A felelős felhasználó azonosítója pozitív egész szám legyen.", "Rendben");
                return;
            }
        }
        else if (_isCurrentUserAdmin)
        {
            SelectedTicket.AssignedUserId = null;
            SelectedTicket.AssignedUserName = null;
        }

        var summaryNote = $"[Munkalap frissítve] Cím: {SelectedTicket.Title} | Kategória: {SelectedTicket.CategoryName} | Helyszín: {SelectedTicket.Location} | Prioritás: {SelectedTicket.Priority} | Felelős ID: {(SelectedTicket.AssignedUserId?.ToString() ?? "nincs")}";
        var noteSaved = await _api.AddTicketNoteAsync(SelectedTicket.Id, summaryNote);

        OnPropertyChanged(nameof(SelectedTicket));
        RefreshPermissionStates();
        await LoadNotesAsync();

        if (noteSaved)
            await Shell.Current.DisplayAlert("Mentve", "A munkalap adatai frissítve és naplózva lettek.", "Rendben");
        else
            await Shell.Current.DisplayAlert("Frissítve", "A munkalap adatai helyben frissítve lettek.", "Rendben");
    }

    [RelayCommand]
    public async Task AddNoteAsync()
    {
        if (SelectedTicket == null || string.IsNullOrWhiteSpace(NewNote)) return;
        if (await _api.AddTicketNoteAsync(SelectedTicket.Id, NewNote.Trim()))
        {
            NewNote = string.Empty;
            await RefreshTicketAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A bejegyzés elküldése nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task CloseTicketAsync()
    {
        if (SelectedTicket == null || string.IsNullOrWhiteSpace(Resolution))
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A lezáró bejegyzés kötelező.", "Rendben");
            return;
        }

        if (await _api.CloseTicketAsync(SelectedTicket.Id, ClosureTypeId, Resolution.Trim()))
        {
            Resolution = string.Empty;
            await RefreshTicketAsync();
            await Shell.Current.DisplayAlert("Sikeres", "A hibajegy lezárult.", "Rendben");
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A lezárás nem sikerült.", "Rendben");
        }
    }

    private async Task RefreshTicketAsync()
    {
        if (SelectedTicket != null && !string.IsNullOrWhiteSpace(SelectedTicket.TicketNumber))
        {
            var fresh = await _api.GetTicketAsync(SelectedTicket.TicketNumber);
            if (fresh != null)
            {
                selectedTicket = fresh;
                OnPropertyChanged(nameof(SelectedTicket));
                RefreshPermissionStates();
                PopulateFormFromTicket(fresh);
            }
            else
            {
                OnPropertyChanged(nameof(SelectedTicket));
                RefreshPermissionStates();
            }
        }

        await LoadNotesAsync();
    }

    [RelayCommand]
    public async Task Close() => await Shell.Current.GoToAsync("..");
}