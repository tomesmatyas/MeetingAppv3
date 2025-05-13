using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeetingApp.Models.ViewModels;

[QueryProperty(nameof(MeetingId), "id")]
public partial class MeetingDetailViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;

    [ObservableProperty] private int meetingId;
    [ObservableProperty] private Meeting? selectedMeeting;
    [ObservableProperty] private ObservableCollection<User> participants = new();
    [ObservableProperty] private string? title;
    [ObservableProperty] private DateTime date;
    [ObservableProperty] private TimeSpan startTime;
    [ObservableProperty] private TimeSpan endTime;
    [ObservableProperty] private string pattern = "None";
    [ObservableProperty] private DateTime? endDate;
    [ObservableProperty] private string fullname = "None";
    [ObservableProperty] private ObservableCollection<User> allUsers = new();
    [ObservableProperty] private User? selectedUserToAdd;
    [ObservableProperty] private string searchText;
    [ObservableProperty] private ObservableCollection<User> filteredUsers = new();
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
        Debug.WriteLine($"[LoadAsync] načítaní: {meeting}");
        if (meeting != null)
        {
            _isInitializing = true;
            SelectedMeeting = meeting;
            Title = meeting.Title;
            Date = meeting.Date;
            StartTime = meeting.StartTime;
            EndTime = meeting.EndTime;
            Pattern = meeting.Recurrence?.Pattern ?? "None";
            EndDate = meeting.EndDate ?? DateTime.Today.AddMonths(1);
            _isInitializing = false;

            var validParticipants = meeting.Participants?
                .Where(p => p?.User != null)
                .Select(p => p.User!)
                .ToList() ?? new List<User>();

            Participants = new ObservableCollection<User>(validParticipants);

            var all = await _meetingService.GetAllUsersAsync();
            var existingIds = validParticipants.Select(p => p.Id).ToHashSet();
            AllUsers = new ObservableCollection<User>(all.Where(u => !existingIds.Contains(u.Id)));
            Debug.WriteLine($"[LoadAsync] AllUsers count: {AllUsers.Count}");
            foreach (var u in AllUsers)
                Debug.WriteLine($"[LoadAsync] User: {u.FullName}");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        Debug.WriteLine($"[Search] hledám: {value}");

        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredUsers = new ObservableCollection<User>(AllUsers);
            Debug.WriteLine("[Search] Výchozí filtr - všichni uživatelé:");
            foreach (var u in FilteredUsers)
                Debug.WriteLine($"→ {u.FullName}");
            return;
        }

        var results = AllUsers
            .Where(u => u.FullName.Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredUsers = new ObservableCollection<User>(results);
        Debug.WriteLine($"[Search] Výsledků: {results.Count}");
        foreach (var u in results)
            Debug.WriteLine($"→ {u.FullName}");
    }

    [RelayCommand]
    private async Task SelectUser(User user)
    {
        if (user != null && !Participants.Any(p => p.Id == user.Id))
        {
            Participants.Add(user);
            AllUsers.Remove(user);
            FilteredUsers.Clear();
            SearchText = string.Empty;

            var participant = new MeetingParticipant { MeetingId = MeetingId, UserId = user.Id };
            await _meetingService.AddParticipantAsync(MeetingId, participant);
        }
    }

    [RelayCommand]
    public async Task RemoveParticipantAsync(User user)
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

        if (Pattern == "None")
        {
            SelectedMeeting.IsRegular = false;
            SelectedMeeting.Recurrence = null;
            SelectedMeeting.EndDate = DateTime.Today;
        }
        else
        {
            SelectedMeeting.IsRegular = true;
            if (SelectedMeeting.Recurrence == null)
                SelectedMeeting.Recurrence = new MeetingRecurrence();

            SelectedMeeting.Recurrence.Pattern = Pattern;
            SelectedMeeting.Recurrence.Interval = 1;
            SelectedMeeting.EndDate = EndDate;
        }

        // ⛔ Zabrání nekonečné rekurzi při serializaci JSON
        if (SelectedMeeting?.Recurrence?.Meetings != null)
            SelectedMeeting.Recurrence.Meetings = null;

        var meetingJson = JsonSerializer.Serialize(SelectedMeeting, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve
        });

        Debug.WriteLine($"[SaveAsync] Recurence: {meetingJson}");

        var success = await _meetingService.UpdateMeetingAsync(SelectedMeeting);
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
        }
    }

    [ObservableProperty] private bool showEndDate = false;

    partial void OnPatternChanged(string value)
    {
        if (_isInitializing) return;
        if (value == "None")
        {
            EndDate = DateTime.Today;
        }
        else
        {
            var today = DateTime.Today;
            EndDate = value == "Weekly" ? today.AddMonths(1) : today.AddMonths(2);

            if (EndDate <= today)
            {
                Shell.Current.DisplayAlert("Neplatné datum", "Zadejte datum v budoucnosti.", "OK");
                EndDate = today.AddDays(7);
            }
        }

        SaveChangesAsync();
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        if (_isInitializing) return;
        SaveChangesAsync();
    }

    private void ValidateEndDate(DateTime selected)
    {
        if (selected <= DateTime.Today)
        {
            Shell.Current.DisplayAlert("Chyba", "Datum, do kdy se má schůzka, musí být později než dnešní!", "OK");
        }
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

        var participant = new MeetingParticipant
        {
            MeetingId = MeetingId,
            UserId = SelectedUserToAdd.Id,
            User = SelectedUserToAdd
        };

        await _meetingService.AddParticipantAsync(MeetingId, participant);

        if (!Participants.Any(p => p.Id == SelectedUserToAdd.Id))
            Participants.Add(SelectedUserToAdd);
    }
}

public static class RecurrencePatterns
{
    public static readonly List<string> All = new() { "None", "Weekly", "Monthly" };
}
