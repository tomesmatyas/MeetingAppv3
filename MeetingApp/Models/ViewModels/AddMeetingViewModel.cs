// File: AddMeetingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class AddMeetingViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;
    private readonly UserSession _session;

    public ObservableCollection<string> RecurrenceOptions { get; } =
        new() { "Weekly", "Monthly" };

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private TimeSpan startTime = new(10, 0, 0);
    [ObservableProperty] private TimeSpan endTime = new(11, 0, 0);
    [ObservableProperty] private string colorHex = "#0078D7";
    [ObservableProperty] private bool isRegular;
    [ObservableProperty] private string? selectedRecurrence;
    [ObservableProperty] private DateTime? endDate;

    [ObservableProperty] private ObservableCollection<UserDto> availableUsers = new();
    [ObservableProperty] private ObservableCollection<UserDto> selectedUsers = new();
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<UserDto> filteredUsers = new();

    public AddMeetingViewModel(MeetingService service, UserSession session)
    {
        _meetingService = service;
        _session = session;
        LoadUsers();
    }

    private async void LoadUsers()
    {
        try
        {
            var users = await _meetingService.GetAllUsersAsync();
            AvailableUsers = new ObservableCollection<UserDto>(users);
            FilteredUsers.Clear();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Chyba] Naèítání uživatelù selhalo: " + ex.Message);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredUsers.Clear();

        if (string.IsNullOrWhiteSpace(value))
            return;

        var filtered = AvailableUsers
            .Where(u =>
                !SelectedUsers.Any(s => s.Id == u.Id) && 
                ($"{u.FirstName} {u.LastName}").Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var user in filtered)
            FilteredUsers.Add(user);
    }

    [RelayCommand]
    private void ToggleUserSelection(UserDto user)
    {
        var existing = SelectedUsers.FirstOrDefault(x => x.Id == user.Id);
        if (existing is not null)
        {
            SelectedUsers.Remove(existing);
        }
        else
        {
            SelectedUsers.Add(user);
        }

        
        OnSearchTextChanged(SearchText);
        SearchText = "";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || StartTime >= EndTime)
        {
            await Shell.Current.DisplayAlert("Chyba", "Zkontrolujte název a èasy schùzky.", "OK");
            return;
        }

        var meeting = new MeetingDto
        {
            Title = Title,
            Date = Date,
            StartTime = StartTime,
            EndTime = EndTime,
            ColorHex = ColorHex,
            IsRegular = IsRegular,
            EndDate = IsRegular ? EndDate : null,
            CreatedByUserId = _session.UserId ?? 0,
            Recurrence = IsRegular && SelectedRecurrence != null
                ? new MeetingRecurrenceDto { Pattern = SelectedRecurrence, Interval = 1 }
                : null,
            Participants = SelectedUsers.Select(u => new MeetingParticipantDto
            {
                UserId = u.Id,
                User = u
            }).ToList()
        };

        try
        {
            Debug.WriteLine(meeting.Participants.Count);
            await _meetingService.CreateMeetingAsync(meeting);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Chyba] " + ex.Message);
        }
    }
}
