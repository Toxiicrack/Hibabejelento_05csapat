using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hibabejelento.Maui.Models;
using Hibabejelento.Maui.Services;
using Location = Hibabejelento.Maui.Models.Location;

namespace Hibabejelento.Maui.ViewModels;

public partial class AdminTicketsViewModel : BaseViewModel
{
    private readonly IApiService _api;

    // ==========================================
    // 0. TAB KEZELÉS
    // ==========================================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTicketsTab))]
    [NotifyPropertyChangedFor(nameof(IsUsersTab))]
    [NotifyPropertyChangedFor(nameof(IsGroupsTab))]
    [NotifyPropertyChangedFor(nameof(IsCategoriesTab))]
    [NotifyPropertyChangedFor(nameof(IsLocationsTab))]
    [NotifyPropertyChangedFor(nameof(IsPrioritiesTab))]
    [NotifyPropertyChangedFor(nameof(TicketsTabBg))]
    [NotifyPropertyChangedFor(nameof(TicketsTabFg))]
    [NotifyPropertyChangedFor(nameof(UsersTabBg))]
    [NotifyPropertyChangedFor(nameof(UsersTabFg))]
    [NotifyPropertyChangedFor(nameof(GroupsTabBg))]
    [NotifyPropertyChangedFor(nameof(GroupsTabFg))]
    [NotifyPropertyChangedFor(nameof(CategoriesTabBg))]
    [NotifyPropertyChangedFor(nameof(CategoriesTabFg))]
    [NotifyPropertyChangedFor(nameof(LocationsTabBg))]
    [NotifyPropertyChangedFor(nameof(LocationsTabFg))]
    [NotifyPropertyChangedFor(nameof(PrioritiesTabBg))]
    [NotifyPropertyChangedFor(nameof(PrioritiesTabFg))]
    private string selectedTab = "Tickets";

    public bool IsTicketsTab => SelectedTab == "Tickets";
    public bool IsUsersTab => SelectedTab == "Users";
    public bool IsGroupsTab => SelectedTab == "Groups";
    public bool IsCategoriesTab => SelectedTab == "Categories";
    public bool IsLocationsTab => SelectedTab == "Locations";
    public bool IsPrioritiesTab => SelectedTab == "Priorities";

    public Color TicketsTabBg => IsTicketsTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color TicketsTabFg => IsTicketsTab ? Colors.White : Color.FromArgb("#4338CA");
    public Color UsersTabBg => IsUsersTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color UsersTabFg => IsUsersTab ? Colors.White : Color.FromArgb("#4338CA");
    public Color GroupsTabBg => IsGroupsTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color GroupsTabFg => IsGroupsTab ? Colors.White : Color.FromArgb("#4338CA");
    public Color CategoriesTabBg => IsCategoriesTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color CategoriesTabFg => IsCategoriesTab ? Colors.White : Color.FromArgb("#4338CA");
    public Color LocationsTabBg => IsLocationsTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color LocationsTabFg => IsLocationsTab ? Colors.White : Color.FromArgb("#4338CA");
    public Color PrioritiesTabBg => IsPrioritiesTab ? Color.FromArgb("#4F46E5") : Color.FromArgb("#EEF2FF");
    public Color PrioritiesTabFg => IsPrioritiesTab ? Colors.White : Color.FromArgb("#4338CA");

    public AdminTicketsViewModel(IApiService api) => _api = api;

    [RelayCommand]
    public async Task SelectTab(string tabName)
    {
        SelectedTab = tabName;
        IsCreateUserPanelVisible = false;
        SelectedUserForGroup = null;
        SelectedTicket = null;
        IsGroupPanelVisible = false;
        IsCategoryPanelVisible = false;
        IsLocationPanelVisible = false;
        IsPriorityPanelVisible = false;
        await LoadCurrentTabAsync();
    }

    public async Task LoadCurrentTabAsync()
    {
        switch (SelectedTab)
        {
            case "Tickets":
                await LoadTicketsAsync();
                break;
            case "Users":
                await LoadUsersAsync();
                break;
            case "Groups":
                await LoadGroupsAsync();
                break;
            case "Categories":
                await LoadCategoriesAsync();
                break;
            case "Locations":
                await LoadLocationsAsync();
                break;
            case "Priorities":
                await LoadPrioritiesAsync();
                break;
        }
    }

    // ==========================================
    // 1. TICKETEK (Hibajegyek)
    // ==========================================
    [ObservableProperty] private ObservableCollection<Ticket> tickets = new();
    [ObservableProperty] private bool showNewOnly = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAcceptPanelVisible))]
    private Ticket? selectedTicket;

    [ObservableProperty] private List<Group> groups = new();
    [ObservableProperty] private List<Priority> priorities = new();
    [ObservableProperty] private List<Category> categories = new();

    [ObservableProperty] private Group? selectedGroup;
    [ObservableProperty] private Priority? selectedPriority;
    [ObservableProperty] private Category? selectedCategory;

    public bool IsAcceptPanelVisible => SelectedTicket != null && SelectedTicket.Status == "Új";

    partial void OnShowNewOnlyChanged(bool value) => _ = LoadTicketsAsync();

    [RelayCommand]
    public async Task LoadTicketsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminTicketsAsync(ShowNewOnly);
            Tickets.Clear();
            foreach (var t in result)
                Tickets.Add(t);

            if (groups.Count == 0)
                Groups = (await _api.GetAdminGroupsAsync())?.Where(g => g.IsActive).ToList() ?? new();

            if (priorities.Count == 0)
                Priorities = (await _api.GetAdminPrioritiesAsync())?.Where(p => p.IsActive).ToList() ?? new();

            if (categories.Count == 0)
            {
                var adminCats = await _api.GetAdminCategoriesAsync();
                Categories = adminCats?.Where(c => c.IsActive).ToList()
                             ?? (await _api.GetCategoriesAsync())?.ToList()
                             ?? new();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectTicket(Ticket? ticket)
    {
        if (ticket == null) return;
        if (ticket.Status != "Új")
        {
            await Shell.Current.DisplayAlert("Nem elfogadható",
                $"Csak 'Új' státuszú ticket fogadható el. Ez a ticket '{ticket.Status}' állapotban van.",
                "Rendben");
            return;
        }

        if (Categories.Count == 0)
        {
            var adminCats = await _api.GetAdminCategoriesAsync();
            Categories = adminCats?.Where(c => c.IsActive).ToList()
                         ?? (await _api.GetCategoriesAsync())?.ToList()
                         ?? new();
        }

        if (Groups.Count == 0)
        {
            Groups = (await _api.GetAdminGroupsAsync())?.Where(g => g.IsActive).ToList() ?? new();
        }

        if (Priorities.Count == 0)
        {
            Priorities = (await _api.GetAdminPrioritiesAsync())?.Where(p => p.IsActive).ToList() ?? new();
        }

        SelectedTicket = ticket;

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == ticket.CategoryId)
                           ?? Categories.FirstOrDefault(c => string.Equals(c.Name, ticket.CategoryName, StringComparison.OrdinalIgnoreCase))
                           ?? Categories.FirstOrDefault();

        SelectedGroup = Groups.FirstOrDefault(g => g.Id == ticket.GroupId);

        SelectedPriority = Priorities.FirstOrDefault(p => p.Id == ticket.PriorityId)
                           ?? Priorities.FirstOrDefault(p => string.Equals(p.Name, ticket.Priority, StringComparison.OrdinalIgnoreCase))
                           ?? Priorities.FirstOrDefault(p => p.IsDefault)
                           ?? Priorities.FirstOrDefault();
    }

    [RelayCommand]
    private void CancelAccept() => SelectedTicket = null;

    [RelayCommand]
    private async Task AcceptTicketAsync()
    {
        if (SelectedTicket == null) return;

        if (SelectedGroup == null)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Válassz csoportot!", "Rendben");
            return;
        }
        if (SelectedCategory == null)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Válassz kategóriát!", "Rendben");
            return;
        }
        if (SelectedPriority == null)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Válassz prioritást!", "Rendben");
            return;
        }

        var request = new AcceptTicketRequest
        {
            GroupId = SelectedGroup.Id,
            CategoryId = SelectedCategory.Id,
            PriorityId = SelectedPriority.Id
        };

        var ok = await _api.AcceptTicketAsync(SelectedTicket.Id, request);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A hibajegyet elfogadtad és csoporthoz rendelted.", "Rendben");
            SelectedTicket = null;
            await LoadTicketsAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az elfogadás nem sikerült.", "Rendben");
        }
    }

    // ==========================================
    // 2. FELHASZNÁLÓK (Users)
    // ==========================================
    [ObservableProperty] private ObservableCollection<UserDto> users = new();
    [ObservableProperty] private bool showPendingUsersOnly = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAssignGroupPanelVisible))]
    private UserDto? selectedUserForGroup;

    [ObservableProperty] private List<Group> availableGroupsForUser = new();
    [ObservableProperty] private Group? selectedGroupForUser;

    public bool IsAssignGroupPanelVisible => SelectedUserForGroup != null;

    // Új felhasználó létrehozása
    [ObservableProperty] private bool isCreateUserPanelVisible = false;
    [ObservableProperty] private string newUserName = string.Empty;
    [ObservableProperty] private string newUserEmail = string.Empty;
    [ObservableProperty] private string newUserPassword = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGroupSelectionVisible))]
    private string newUserRoleType = "Bejelentő";
    public List<string> RoleTypeOptions { get; } = new() { "Bejelentő", "Karbantartó", "Admin" };
    public bool IsGroupSelectionVisible => string.Equals(NewUserRoleType, "Karbantartó", StringComparison.OrdinalIgnoreCase);
    [ObservableProperty] private Group? newUserSelectedGroup;

    partial void OnShowPendingUsersOnlyChanged(bool value) => _ = LoadUsersAsync();

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminUsersAsync(ShowPendingUsersOnly);
            Users.Clear();
            if (result != null)
            {
                foreach (var u in result)
                    Users.Add(u);
            }

            AvailableGroupsForUser = (await _api.GetAdminGroupsAsync())?.Where(g => g.IsActive).ToList() ?? new();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ApproveUserAsync(UserDto? user)
    {
        if (user == null) return;
        if (!user.CanApprove)
        {
            await Shell.Current.DisplayAlert("Nem szükséges", "Ez a felhasználó már jóvá van hagyva.", "Rendben");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan jóváhagyod {user.Name} regisztrációját?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.ApproveUserAsync(user.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A regisztráció jóváhagyva.", "Rendben");
            await LoadUsersAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A jóváhagyás nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task DeactivateUserAsync(UserDto? user)
    {
        if (user == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan inaktiválod {user.Name} fiókját?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.DeactivateUserAsync(user.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A felhasználó inaktiválva lett.", "Rendben");
            await LoadUsersAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Nem sikerült az inaktiválás (pl. utolsó admin nem inaktiválható).", "Rendben");
        }
    }

    [RelayCommand]
    public void OpenAssignGroup(UserDto? user)
    {
        if (user == null) return;
        if (!user.CanAssignGroup)
        {
            Shell.Current.DisplayAlert("Nem megengedett", "Csak a karbantartókat lehet csoportosítani!", "Rendben");
            return;
        }
        IsCreateUserPanelVisible = false;
        SelectedUserForGroup = user;
        SelectedGroupForUser = AvailableGroupsForUser.FirstOrDefault(g => g.Id == user.GroupId);
    }

    [RelayCommand]
    public void CancelAssignGroup() => SelectedUserForGroup = null;

    [RelayCommand]
    public async Task OpenCreateUserAsync()
    {
        SelectedUserForGroup = null;
        NewUserName = string.Empty;
        NewUserEmail = string.Empty;
        NewUserPassword = string.Empty;
        NewUserRoleType = "Bejelentő";
        NewUserSelectedGroup = null;

        if (AvailableGroupsForUser.Count == 0)
        {
            AvailableGroupsForUser = (await _api.GetAdminGroupsAsync())?.Where(g => g.IsActive).ToList() ?? new();
        }

        IsCreateUserPanelVisible = true;
    }

    [RelayCommand]
    public void CancelCreateUser()
    {
        IsCreateUserPanelVisible = false;
    }

    [RelayCommand]
    public async Task SaveCreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUserName) || NewUserName.Trim().Length < 2)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Adj meg egy érvényes nevet (legalább 2 karakter)!", "Rendben");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewUserEmail) || !NewUserEmail.Contains('@'))
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Adj meg egy érvényes e-mail címet!", "Rendben");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewUserPassword) || NewUserPassword.Trim().Length < 4)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Adj meg egy jelszót (legalább 4 karakter)!", "Rendben");
            return;
        }

        if (IsGroupSelectionVisible && NewUserSelectedGroup == null)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "Karbantartó típusú felhasználóhoz válassz csoportot!", "Rendben");
            return;
        }

        int? selectedGid = IsGroupSelectionVisible ? NewUserSelectedGroup?.Id : null;

        bool ok = await _api.CreateUserAsync(NewUserName.Trim(), NewUserEmail.Trim(), NewUserPassword.Trim(), NewUserRoleType, selectedGid);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", $"A(z) {NewUserName} felhasználó sikeresen létrehozva ({NewUserRoleType}).", "Rendben");
            IsCreateUserPanelVisible = false;
            await LoadUsersAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Nem sikerült létrehozni a felhasználót (pl. már létező e-mail cím vagy érvénytelen adatok).", "Rendben");
        }
    }

    [RelayCommand]
    public async Task SaveAssignGroupAsync()
    {
        if (SelectedUserForGroup == null) return;

        int? newGroupId = SelectedGroupForUser?.Id;
        bool ok = await _api.AssignUserGroupAsync(SelectedUserForGroup.Id, newGroupId);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A csoport-hozzárendelés frissítve.", "Rendben");
            SelectedUserForGroup = null;
            await LoadUsersAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A csoport hozzárendelése sikertelen.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task RemoveUserGroupAsync()
    {
        if (SelectedUserForGroup == null) return;
        bool ok = await _api.AssignUserGroupAsync(SelectedUserForGroup.Id, null);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A felhasználó eltávolítva a csoportból.", "Rendben");
            SelectedUserForGroup = null;
            await LoadUsersAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Nem sikerült a csoport eltávolítása.", "Rendben");
        }
    }

    // ==========================================
    // 3. CSOPORTOK (Groups)
    // ==========================================
    [ObservableProperty] private ObservableCollection<Group> groupsList = new();
    [ObservableProperty] private bool isGroupPanelVisible = false;
    [ObservableProperty] private int? editingGroupId;
    [ObservableProperty] private string groupNameInput = string.Empty;
    [ObservableProperty] private string groupDescriptionInput = string.Empty;

    public string GroupPanelTitle => EditingGroupId.HasValue ? "Csoport módosítása" : "Új csoport létrehozása";

    [RelayCommand]
    public async Task LoadGroupsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminGroupsAsync();
            GroupsList.Clear();
            if (result != null)
            {
                foreach (var g in result.Where(g => g.IsActive))
                    GroupsList.Add(g);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenCreateGroup()
    {
        EditingGroupId = null;
        GroupNameInput = string.Empty;
        GroupDescriptionInput = string.Empty;
        OnPropertyChanged(nameof(GroupPanelTitle));
        IsGroupPanelVisible = true;
    }

    [RelayCommand]
    public void OpenEditGroup(Group? group)
    {
        if (group == null) return;
        EditingGroupId = group.Id;
        GroupNameInput = group.Name;
        GroupDescriptionInput = group.Description ?? string.Empty;
        OnPropertyChanged(nameof(GroupPanelTitle));
        IsGroupPanelVisible = true;
    }

    [RelayCommand]
    public void CancelGroupEdit() => IsGroupPanelVisible = false;

    [RelayCommand]
    public async Task SaveGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(GroupNameInput) || GroupNameInput.Trim().Length < 2)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A csoport neve legalább 2 karakter legyen.", "Rendben");
            return;
        }

        bool ok;
        if (EditingGroupId.HasValue)
            ok = await _api.UpdateGroupAsync(EditingGroupId.Value, GroupNameInput.Trim(), GroupDescriptionInput.Trim());
        else
            ok = await _api.CreateGroupAsync(GroupNameInput.Trim(), GroupDescriptionInput.Trim());

        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A csoport mentve.", "Rendben");
            IsGroupPanelVisible = false;
            await LoadGroupsAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A csoport mentése sikertelen (lehet, hogy a név már létezik).", "Rendben");
        }
    }

    [RelayCommand]
    public async Task DeactivateGroupAsync(Group? group)
    {
        if (group == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan inaktiválod a(z) {group.Name} csoportot?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.DeactivateGroupAsync(group.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A csoport inaktiválva.", "Rendben");
            await LoadGroupsAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az inaktiválás sikertelen (aktív tagok vannak a csoportban!).", "Rendben");
        }
    }

    // ==========================================
    // 4. KATEGÓRIÁK (Categories)
    // ==========================================
    [ObservableProperty] private ObservableCollection<Category> categoriesList = new();
    [ObservableProperty] private bool isCategoryPanelVisible = false;
    [ObservableProperty] private string categoryNameInput = string.Empty;
    [ObservableProperty] private string categoryDescriptionInput = string.Empty;

    [RelayCommand]
    public async Task LoadCategoriesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminCategoriesAsync();
            CategoriesList.Clear();
            if (result != null)
            {
                foreach (var c in result.Where(c => c.IsActive))
                    CategoriesList.Add(c);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenCreateCategory()
    {
        CategoryNameInput = string.Empty;
        CategoryDescriptionInput = string.Empty;
        IsCategoryPanelVisible = true;
    }

    [RelayCommand]
    public void CancelCategoryEdit() => IsCategoryPanelVisible = false;

    [RelayCommand]
    public async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(CategoryNameInput) || CategoryNameInput.Trim().Length < 2)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A kategória neve legalább 2 karakter legyen.", "Rendben");
            return;
        }

        bool ok = await _api.CreateCategoryAsync(CategoryNameInput.Trim(), CategoryDescriptionInput.Trim());
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A kategória létrehozva.", "Rendben");
            IsCategoryPanelVisible = false;
            await LoadCategoriesAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A kategória mentése sikertelen.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task DeactivateCategoryAsync(Category? category)
    {
        if (category == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan inaktiválod a(z) {category.Name} kategóriát?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.DeactivateCategoryAsync(category.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A kategória inaktiválva.", "Rendben");
            await LoadCategoriesAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az inaktiválás nem sikerült.", "Rendben");
        }
    }

    // ==========================================
    // 5. HELYSZÍNEK (Locations)
    // ==========================================
    [ObservableProperty] private ObservableCollection<Location> locationsList = new();
    [ObservableProperty] private bool isLocationPanelVisible = false;
    [ObservableProperty] private int? editingLocationId;
    [ObservableProperty] private string locationNameInput = string.Empty;
    [ObservableProperty] private string locationDescriptionInput = string.Empty;

    public string LocationPanelTitle => EditingLocationId.HasValue ? "Helyszín módosítása" : "Új helyszín létrehozása";

    [RelayCommand]
    public async Task LoadLocationsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminLocationsAsync();
            LocationsList.Clear();
            if (result != null)
            {
                foreach (var l in result.Where(l => l.IsActive))
                    LocationsList.Add(l);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenCreateLocation()
    {
        EditingLocationId = null;
        LocationNameInput = string.Empty;
        LocationDescriptionInput = string.Empty;
        OnPropertyChanged(nameof(LocationPanelTitle));
        IsLocationPanelVisible = true;
    }

    [RelayCommand]
    public void OpenEditLocation(Location? loc)
    {
        if (loc == null) return;
        EditingLocationId = loc.Id;
        LocationNameInput = loc.Name;
        LocationDescriptionInput = loc.Description ?? string.Empty;
        OnPropertyChanged(nameof(LocationPanelTitle));
        IsLocationPanelVisible = true;
    }

    [RelayCommand]
    public void CancelLocationEdit() => IsLocationPanelVisible = false;

    [RelayCommand]
    public async Task SaveLocationAsync()
    {
        if (string.IsNullOrWhiteSpace(LocationNameInput) || LocationNameInput.Trim().Length < 2)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A helyszín neve legalább 2 karakter legyen.", "Rendben");
            return;
        }

        bool ok;
        if (EditingLocationId.HasValue)
            ok = await _api.UpdateLocationAsync(EditingLocationId.Value, LocationNameInput.Trim(), LocationDescriptionInput.Trim());
        else
            ok = await _api.CreateLocationAsync(LocationNameInput.Trim(), LocationDescriptionInput.Trim());

        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A helyszín mentve.", "Rendben");
            IsLocationPanelVisible = false;
            await LoadLocationsAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A helyszín mentése sikertelen.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task DeactivateLocationAsync(Location? loc)
    {
        if (loc == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan inaktiválod a(z) {loc.Name} helyszínt?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.DeactivateLocationAsync(loc.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A helyszín inaktiválva.", "Rendben");
            await LoadLocationsAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az inaktiválás nem sikerült.", "Rendben");
        }
    }

    // ==========================================
    // 6. PRIORITÁSOK (Priorities)
    // ==========================================
    [ObservableProperty] private ObservableCollection<Priority> prioritiesList = new();
    [ObservableProperty] private bool isPriorityPanelVisible = false;
    [ObservableProperty] private int? editingPriorityId;
    [ObservableProperty] private string priorityNameInput = string.Empty;
    [ObservableProperty] private int priorityDueDaysInput = 5;
    [ObservableProperty] private bool priorityIsDefaultInput = false;

    public string PriorityPanelTitle => EditingPriorityId.HasValue ? "Prioritás módosítása" : "Új prioritás létrehozása";

    [RelayCommand]
    public async Task LoadPrioritiesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _api.GetAdminPrioritiesAsync();
            PrioritiesList.Clear();
            if (result != null)
            {
                foreach (var p in result.Where(p => p.IsActive))
                    PrioritiesList.Add(p);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenCreatePriority()
    {
        EditingPriorityId = null;
        PriorityNameInput = string.Empty;
        PriorityDueDaysInput = 5;
        PriorityIsDefaultInput = false;
        OnPropertyChanged(nameof(PriorityPanelTitle));
        IsPriorityPanelVisible = true;
    }

    [RelayCommand]
    public void OpenEditPriority(Priority? p)
    {
        if (p == null) return;
        EditingPriorityId = p.Id;
        PriorityNameInput = p.Name;
        PriorityDueDaysInput = p.DefaultDueDays;
        PriorityIsDefaultInput = p.IsDefault;
        OnPropertyChanged(nameof(PriorityPanelTitle));
        IsPriorityPanelVisible = true;
    }

    [RelayCommand]
    public void CancelPriorityEdit() => IsPriorityPanelVisible = false;

    [RelayCommand]
    public async Task SavePriorityAsync()
    {
        if (string.IsNullOrWhiteSpace(PriorityNameInput) || PriorityNameInput.Trim().Length < 2)
        {
            await Shell.Current.DisplayAlert("Hiányzó adat", "A prioritás neve legalább 2 karakter legyen.", "Rendben");
            return;
        }

        if (PriorityDueDaysInput < 1 || PriorityDueDaysInput > 365)
        {
            await Shell.Current.DisplayAlert("Érvénytelen határidő", "A határidő 1 és 365 munkanap között legyen.", "Rendben");
            return;
        }

        bool ok;
        if (EditingPriorityId.HasValue)
            ok = await _api.UpdatePriorityAsync(EditingPriorityId.Value, PriorityNameInput.Trim(), PriorityDueDaysInput, PriorityIsDefaultInput);
        else
            ok = await _api.CreatePriorityAsync(PriorityNameInput.Trim(), PriorityDueDaysInput, PriorityIsDefaultInput);

        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A prioritás mentve.", "Rendben");
            IsPriorityPanelVisible = false;
            await LoadPrioritiesAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "A prioritás mentése sikertelen.", "Rendben");
        }
    }

    [RelayCommand]
    public async Task DeactivatePriorityAsync(Priority? p)
    {
        if (p == null) return;
        if (p.IsDefault)
        {
            await Shell.Current.DisplayAlert("Figyelmeztetés", "Az alapértelmezett prioritást nem lehet inaktiválni! Előbb jelölj ki egy másikat alapértelmezettnek.", "Rendben");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlert("Megerősítés", $"Biztosan inaktiválod a(z) {p.Name} prioritást?", "Igen", "Nem");
        if (!confirm) return;

        bool ok = await _api.DeactivatePriorityAsync(p.Id);
        if (ok)
        {
            await Shell.Current.DisplayAlert("Sikeres", "A prioritás inaktiválva.", "Rendben");
            await LoadPrioritiesAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("Hiba", "Az inaktiválás nem sikerült.", "Rendben");
        }
    }

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");
}
