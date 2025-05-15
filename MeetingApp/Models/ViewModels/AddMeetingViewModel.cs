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

public partial class AddMeetingViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;
    private readonly UserSession _userSession;

    public ObservableCollection<UserDto> AllUsers { get; } = new();
    public ObservableCollection<UserDto> SelectedParticipants { get; } = new();
    public ObservableCollection<UserDto> FilteredUsers { get; } = new();
    public ObservableCollection<string> RecurrencePatterns { get; } = new() { "Vyberte prosím", "Weekly", "Monthly" };

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private TimeSpan startTime = new(9, 0, 0);
    [ObservableProperty] private TimeSpan endTime = new(10, 0, 0);
    [ObservableProperty] private string colorHex = "#0078D7";
    [ObservableProperty] private bool isRegular = false;
    [ObservableProperty] private string selectedPattern = "Vyberte prosím";
    [ObservableProperty] private DateTime? endDate = null;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private UserDto? selectedUserToAdd;
    [ObservableProperty] private ObservableCollection<UserDto> filteredUsers = new();

    public AddMeetingViewModel(MeetingService meetingService, UserSession userSession)
    {
        _meetingService = meetingService;
        _userSession = userSession;
        LoadUsers();
    }

    private async void LoadUsers()
    {
        var users = await _meetingService.GetAllUsersAsync();
        AllUsers.Clear();
        foreach (var user in users)
            AllUsers.Add(user);
        FilteredUsers.Clear();
        foreach (var user in users)
            FilteredUsers.Add(user);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredUsers = new ObservableCollection<UserDto>(AllUsers);
            return;
        }

        FilteredUsers = new ObservableCollection<UserDto>(
            AllUsers.Where(u => ($"{u.FirstName} {u.LastName}").Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private async Task SelectUser(UserDto user)
    {
        if (!Participants.Any(p => p.Id == user.Id))
        {
            Participants.Add(user);
            AllUsers.Remove(user);
            FilteredUsers.Clear();
            SearchText = string.Empty;

            await _meetingService.AddParticipantAsync(MeetingId, user.Id);
        }
    }

    [RelayCommand]
    public async Task SaveMeetingAsync()
    {
        if (startTime >= endTime)
        {
            await Shell.Current.DisplayAlert("Chyba", "Zaèátek musí být pøed koncem.", "OK");
            return;
        }

        if (_userSession.UserId == null)
        {
            await Shell.Current.DisplayAlert("Chyba", "Uživatel není pøihlášen.", "OK");
            return;
        }

        var meeting = new MeetingDto
        {
            Title = title,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            ColorHex = colorHex,
            IsRegular = isRegular,
            CreatedByUserId = _userSession.UserId.Value,
            Participants = SelectedParticipants.Select(u => new MeetingParticipantDto { UserId = u.Id, User = u }).ToList()
        };

        if (isRegular && selectedPattern != "Vyberte prosím")
        {
            meeting.Recurrence = new MeetingRecurrenceDto
            {
                Pattern = selectedPattern,
                Interval = 1
            };
            meeting.EndDate = endDate ?? date.AddMonths(1);
        }
        if (isRegular && selectedPattern != "Vyberte prosím")
        {
            var result = await _meetingService.AddMeetingAsync(meeting);
            if (result)
                await Shell.Current.GoToAsync("..", true);
            else
                await Shell.Current.DisplayAlert("Upozornìní", "Schùzka bude synchronizována pozdìji.", "OK");
        }
        else
            await Shell.Current.DisplayAlert("Upozornìní", "Vyberte správné opakování", "OK");
    }
}
