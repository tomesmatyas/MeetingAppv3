// File: MeetingDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using MeetingApp.Services.Helper;

namespace MeetingApp.Models.ViewModels;

[QueryProperty(nameof(MeetingId), "id")]
public partial class MeetingDetailViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;

    [ObservableProperty] private int meetingId;
    [ObservableProperty] private MeetingDto? selectedMeeting;
    [ObservableProperty] private ObservableCollection<UserDto> participants = new();
    [ObservableProperty] private string? title;
    [ObservableProperty] private DateTime date;
    [ObservableProperty] private TimeSpan startTime;
    [ObservableProperty] private TimeSpan endTime;
    [ObservableProperty] private string pattern = "Ne";
    [ObservableProperty] private DateTime? endDate;
    [ObservableProperty] private bool isRegular;
    [ObservableProperty] private string fullname = "None";
    [ObservableProperty] private ObservableCollection<UserDto> allUsers = new();
    [ObservableProperty] private UserDto? selectedUserToAdd;
    [ObservableProperty] private string searchText;
    [ObservableProperty] private ObservableCollection<UserDto> filteredUsers = new();
    [ObservableProperty] private int interval = 1;
 

    public List<ColorOption> AvailableColors { get; } = new()
{
    new ColorOption { Name = "Modrá", Hex = "#0078D7" },
    new ColorOption { Name = "Červená", Hex = "#D32F2F" },
    new ColorOption { Name = "Zelená", Hex = "#388E3C" },
    new ColorOption { Name = "Žlutá", Hex = "#FBC02D" },
    new ColorOption { Name = "Fialová", Hex = "#7B1FA2" },
    new ColorOption { Name = "Tyrkysová", Hex = "#0288D1" },
    new ColorOption { Name = "Oranžová", Hex = "#FFA000" },
};

    // Property pro vybranou barvu:
    [ObservableProperty]
    private ColorOption selectedColor;

    // Když potřebuješ HEX do modelu při ukládání:
    public string ColorHex => SelectedColor?.Hex;


    private bool _isInitializing = true;

    public List<string> RecurrencePatterns => new() { "Týden", "Měsíc" };

    public MeetingDetailViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
        _ = LoadAsync();
        SelectedColor = AvailableColors.FirstOrDefault();
    }

    public async Task LoadAsync()
    {
        await _meetingService.InitAsync();
        if (MeetingId <= 0) return;

        var meeting = await _meetingService.GetMeetingByIdAsync(MeetingId);
        if (meeting == null) return;

        Debug.WriteLine($"Načtená schůzka: {meeting.Title} na {meeting.Date:dd.MM.yyyy}");
        foreach (var p in meeting.Participants)
        {
            Debug.WriteLine(p == null
                ? "[DEBUG] Null participant"
                : $"[DEBUG] Participant {p} - {p.User.Username} - {p.User.FirstName} {p.User.LastName}");
        }
       

        _isInitializing = true;

        SelectedMeeting = meeting;
        Title = meeting.Title;
        Date = meeting.Date;
        StartTime = meeting.StartTime;
        EndTime = meeting.EndTime;
        IsRegular = meeting.IsRegular;
        SelectedColor = AvailableColors.FirstOrDefault(c => c.Hex == meeting.ColorHex) ?? AvailableColors.First();
        Pattern = meeting.IsRegular
            ? RecurrencePatterns.FirstOrDefault(p => p.Equals(meeting.RecurrencePattern, StringComparison.OrdinalIgnoreCase)) ?? "Týden"
            : "Ne";
        EndDate = meeting.EndDate;
        Interval = meeting.Interval;

        Participants = new ObservableCollection<UserDto>(
            meeting.Participants
                .Where(p => p.User != null)
                .Select(p => p.User!));

        var all = await _meetingService.GetAllUsersAsync();
        var existingIds = Participants.Select(p => p.Id).ToHashSet();
        AllUsers = new ObservableCollection<UserDto>(all.Where(u => !existingIds.Contains(u.Id)));

        _isInitializing = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredUsers = string.IsNullOrWhiteSpace(value)
            ? new ObservableCollection<UserDto>(AllUsers)
            : new ObservableCollection<UserDto>(
                AllUsers.Where(u => ($"{u.FirstName} {u.LastName}")
                    .Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    partial void OnIsRegularChanged(bool value)
    {
        if (_isInitializing) return;

        Pattern = value ? "Týden" : "Ne";
        EndDate = value ? DateTime.Today.AddMonths(1) : null;
        _ = SaveChangesAsync();
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
    public async Task RemoveParticipantAsync(UserDto user)
    {
        await _meetingService.RemoveParticipantAsync(MeetingId, user.Id);
        Participants.Remove(user);
        if (!AllUsers.Any(u => u.Id == user.Id))
            AllUsers.Add(user);
    }

    [RelayCommand]
    public async Task SaveChangesButtonAsync()
    {
        await SaveChangesAsync();
        WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
        await Shell.Current.GoToAsync("..", true);
    }

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        if (SelectedMeeting == null) return;

        SelectedMeeting.Title = Title;
        SelectedMeeting.Date = Date;
        SelectedMeeting.StartTime = StartTime;
        SelectedMeeting.EndTime = EndTime;
        SelectedMeeting.IsRegular = IsRegular;
        SelectedMeeting.EndDate = EndDate;
        SelectedMeeting.ColorHex = SelectedColor.Hex;
        SelectedMeeting.Interval = Interval;
        SelectedMeeting.RecurrenceId = Pattern switch
        {
            "Týden" => 2,
            "Měsíc" => 3,
            _ => 1
        };
        SelectedMeeting.Interval = IsRegular ? 1 : 0;

        await _meetingService.UpdateMeetingAsync(SelectedMeeting);
    }

    [RelayCommand]
    public async Task DeleteMeetingAsync()
    {
        if (SelectedMeeting == null) return;

        var success = await _meetingService.DeleteMeetingAsync(SelectedMeeting.Id);
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
            MeetingNotificationHelper.CancelNotification(SelectedMeeting.Id);
            await Shell.Current.GoToAsync("..", true);
        }
    }

    partial void OnPatternChanged(string value)
    {
        if (_isInitializing) return;

        var today = DateTime.Today;
        EndDate = value switch
        {
            "Týden" => today.AddMonths(1),
            "Měsíc" => today.AddMonths(2),
            _ => null
        };

        _ = SaveChangesAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        if (_isInitializing || SelectedMeeting == null) return;
        SelectedMeeting.EndDate = value;
        _ = SaveChangesAsync();
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await Shell.Current.GoToAsync("..", true);
    }

    [RelayCommand]
    public async Task AddParticipantAsync()
    {
        if (SelectedUserToAdd == null) return;
        await _meetingService.AddParticipantAsync(MeetingId, SelectedUserToAdd.Id);
        if (!Participants.Any(p => p.Id == SelectedUserToAdd.Id))
            Participants.Add(SelectedUserToAdd);
    }
}
