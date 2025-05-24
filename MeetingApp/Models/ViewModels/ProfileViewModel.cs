using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MeetingApp.Models.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly MeetingService _meetingService; // používáš MeetingService místo UserService!

    // Všichni uživatelé (kromě adminů)
    private List<UserDto> _allUsers = new();

    [ObservableProperty]
    private UserDto? currentUser;

    [ObservableProperty]
    private ObservableCollection<UserDto> myUsers = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<UserDto> filteredUsers = new();

    public bool IsAdmin => CurrentUser?.Role?.ToLower() == "admin";

    public string FullName =>
        string.IsNullOrWhiteSpace(CurrentUser?.FirstName) && string.IsNullOrWhiteSpace(CurrentUser?.LastName)
            ? CurrentUser?.Username ?? ""
            : $"{CurrentUser?.FirstName} {CurrentUser?.LastName}".Trim();

    public ProfileViewModel(IAuthService authService, MeetingService meetingService)
    {
        _authService = authService;
        _meetingService = meetingService;
    }

    public async Task InitAsync()
    {
        CurrentUser = _authService.GetCurrentUser();

        if (IsAdmin)
        {
            // 1. Moji aktuální uživatelé
            MyUsers = new ObservableCollection<UserDto>(await _meetingService.GetMyUsersAsync());

            // 2. Všichni uživatelé (ale ne admini!)
            var allUsers = await _meetingService.GetAllUsersAsync();
            _allUsers = allUsers
                .Where(u => !string.Equals(u.Role, "admin", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var user in _allUsers)
            {
                Debug.WriteLine($"ID: {user.Id}, Username: {user.Username}, Jméno: {user.FirstName} {user.LastName}, Role: {user.Role}");
            }
            FilterUsers();
        }
    }

    // Vyhledávání – dynamicky filtruje uživatele podle textu a ti, co už jsou přiřazení, nezobrazuje
    partial void OnSearchTextChanged(string value)
    {
        FilterUsers();
        
    }

    private void FilterUsers()
    {
        if (!IsAdmin)
        {
            FilteredUsers.Clear();
            return;
        }

        // Pokud je SearchText prázdný, zobraz prázdný seznam
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredUsers.Clear();
            return;
        }

        var notMyUsers = _allUsers
            .Where(u => !MyUsers.Any(m => m.Id == u.Id))
            .ToList();

        // Filtruj pouze podle hledání
        notMyUsers = notMyUsers
            .Where(u =>
                (u.FullName ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (u.Username ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredUsers = new ObservableCollection<UserDto>(notMyUsers);
        
    }

    [RelayCommand]
    private async Task AddUserAsync(UserDto user)
    {
        if (await _meetingService.AddUserToAdminAsync(user.Id))
        {
            MyUsers.Add(user);
            FilterUsers();
        }
        SearchText = "";
    }

    [RelayCommand]
    private async Task RemoveUserAsync(UserDto user)
    {
        if (await _meetingService.RemoveUserFromAdminAsync(user.Id))
        {
            MyUsers.Remove(user);
            FilterUsers();
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
    }
}
