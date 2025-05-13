// MeetingDetailViewModel.cs - updated to use DTOs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

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
    [ObservableProperty] private string pattern = "None";
    [ObservableProperty] private DateTime? endDate;
    [ObservableProperty] private string fullname = "None";
    [ObservableProperty] private ObservableCollection<UserDto> allUsers = new();
    [ObservableProperty] private UserDto? selectedUserToAdd;
    [ObservableProperty] private string searchText;
    [ObservableProperty] private ObservableCollection<UserDto> filteredUsers = new();

    private bool _isInitializing = true;

    public List<string> RecurrencePatterns => new() { "None", "Weekly", "Monthly" };

    public MeetingDetailViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    public async Task LoadAsync()
    {
        if (MeetingId <= 0) return;

        var meeting = await _meetingService.GetMeetingByIdAsync(MeetingId);
        Debug.WriteLine($"Načtená schůzka: {meeting.Title} na {meeting.Date:dd.MM.yyyy}");
        if (meeting != null)
        {
            _isInitializing = true;

            SelectedMeeting = meeting;
            Title = meeting.Title;
            Date = meeting.Date;
            StartTime = meeting.StartTime;
            EndTime = meeting.EndTime;
            Pattern = meeting.Recurrence?.Pattern ?? "None";
            EndDate = meeting.EndDate;

            Participants = new ObservableCollection<UserDto>(meeting.Participants);
            Debug.WriteLine($"Načtená schůzka: {meeting.Title} na {meeting.Date:dd.MM.yyyy}");
            var all = await _meetingService.GetAllUsersAsync();
            var existingIds = meeting.Participants.Select(p => p.Id).ToHashSet();
            AllUsers = new ObservableCollection<UserDto>(all.Where(u => !existingIds.Contains(u.Id)));

            _isInitializing = false;
        }
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
    public async Task RemoveParticipantAsync(UserDto user)
    {
        await _meetingService.RemoveParticipantAsync(MeetingId, user.Id);
        Participants.Remove(user);

        if (!AllUsers.Any(u => u.Id == user.Id))
            AllUsers.Add(user);
    }

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        if (SelectedMeeting == null) return;

        SelectedMeeting.Title = Title;
        SelectedMeeting.Date = Date;
        SelectedMeeting.StartTime = StartTime;
        SelectedMeeting.EndTime = EndTime;
        SelectedMeeting.IsRegular = Pattern != "None";
        SelectedMeeting.EndDate = EndDate;

        if (SelectedMeeting.IsRegular)
        {
            SelectedMeeting.Recurrence ??= new MeetingRecurrenceDto();
            SelectedMeeting.Recurrence.Pattern = Pattern;
            SelectedMeeting.Recurrence.Interval = 1;
        }
        else
        {
            SelectedMeeting.Recurrence = null;
        }

        await _meetingService.UpdateMeetingAsync(SelectedMeeting);
    }

    partial void OnPatternChanged(string value)
    {
        if (_isInitializing) return;

        var today = DateTime.Today;
        EndDate = value switch
        {
            "Weekly" => today.AddMonths(1),
            "Monthly" => today.AddMonths(2),
            _ => today
        };

        if (EndDate <= today)
            EndDate = today.AddDays(7);

        SaveChangesAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        if (_isInitializing) return;
        SaveChangesAsync();
    }

    [RelayCommand]
    public async Task DeleteMeetingAsync()
    {
        if (SelectedMeeting != null)
        {
            var success = await _meetingService.DeleteMeetingAsync(SelectedMeeting.Id);
            if (success)
            {
                WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
                await Shell.Current.GoToAsync("..", true);
            }
        }
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
