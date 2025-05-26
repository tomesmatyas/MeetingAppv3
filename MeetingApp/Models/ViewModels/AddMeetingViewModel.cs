// File: AddMeetingViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using MeetingApp.Services.Helper;
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
    new() { "Týden", "Mìsíc" };

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private DateTime date = DateTime.Today;
    private static TimeSpan RoundUpToHalfHour(DateTime dt) =>
    TimeSpan.FromMinutes(Math.Ceiling(dt.TimeOfDay.TotalMinutes / 30) * 30);

    [ObservableProperty]
    private TimeSpan startTime = RoundUpToHalfHour(DateTime.Now);

    [ObservableProperty]
    private TimeSpan endTime = RoundUpToHalfHour(DateTime.Now).Add(TimeSpan.FromHours(1));
    
    [ObservableProperty] private bool isRegular;
    [ObservableProperty] private string? selectedRecurrence;
    [ObservableProperty] private DateTime? endDate;


    [ObservableProperty] private ObservableCollection<UserDto> availableUsers = new();
    [ObservableProperty] private ObservableCollection<UserDto> selectedUsers = new();
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<UserDto> filteredUsers = new();
    [ObservableProperty] private int interval = 1;

    public List<ColorOption> AvailableColors { get; } = new()
{
    new ColorOption { Name = "Modrá", Hex = "#0078D7" },
    new ColorOption { Name = "Èervená", Hex = "#D32F2F" },
    new ColorOption { Name = "Zelená", Hex = "#388E3C" },
    new ColorOption { Name = "Žlutá", Hex = "#FBC02D" },
    new ColorOption { Name = "Fialová", Hex = "#7B1FA2" },
    new ColorOption { Name = "Tyrkysová", Hex = "#0288D1" },
    new ColorOption { Name = "Oranžová", Hex = "#FFA000" },
};
    [ObservableProperty]
    private ColorOption selectedColor;

    // Když potøebuješ HEX do modelu pøi ukládání:
    public string ColorHex => SelectedColor?.Hex;


    public AddMeetingViewModel(MeetingService service, UserSession session)
    {
        _meetingService = service;
        _session = session;
        LoadUsers();
    }
    public bool IsAdmin => _session.IsAdmin;

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

        var isRecurring = SelectedRecurrence is "Týden" or "Mìsíc";

        var meeting = new MeetingDto
        {
            Title = Title,
            Date = Date,
            StartTime = StartTime,
            EndTime = EndTime,
            ColorHex = SelectedColor.Hex,
            IsRegular = IsRegular,
            EndDate = IsRegular ? EndDate : null,
            CreatedByUserId = _session.UserId ?? 0,
            RecurrenceId = SelectedRecurrence switch
            {
                "Týden" => 2,
                "Mìsíc" => 3,
                _ => 1
            },
            Interval = Interval,
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
            await MeetingNotificationHelper.ScheduleNotificationAsync(meeting);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Chyba] " + ex.Message);
        }
    }
}
