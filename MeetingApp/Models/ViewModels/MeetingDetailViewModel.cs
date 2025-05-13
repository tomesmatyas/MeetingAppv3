using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;


namespace MeetingApp.Models.ViewModels;

[QueryProperty(nameof(MeetingId), "id")]
public partial class MeetingDetailViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;

    [ObservableProperty]
    private int meetingId;

    [ObservableProperty]
    private Meeting? selectedMeeting;

    [ObservableProperty]


    public MeetingDetailViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }
    
    public async Task LoadAsync()
    {
        Debug.WriteLine("Otevírám stránku s detailem");
        if (MeetingId <= 0)
            return;

        var meeting = await _meetingService.GetMeetingByIdAsync(MeetingId);
        Debug.WriteLine($"Naèítám detail pro ID: {MeetingId}");
        Debug.WriteLine($"Naètený meeting: {meeting?.Title}");

        if (meeting != null)
        {
            SelectedMeeting = meeting;
            Title = meeting.Title;
            Date = meeting.Date;
            StartTime = meeting.StartTime;
            EndTime = meeting.EndTime;
            Pattern = meeting.Recurrence?.Pattern ?? "None";
            EndDate = meeting.EndDate ?? null;
            Participants = new ObservableCollection<User>(
                meeting.Participants.Select(p => p.User).Where(u => u != null)!);

            Debug.WriteLine("Úèastníci:");
            foreach (var p in meeting.Participants)
            {
                Debug.WriteLine($"{p.User?.FirstName} {p.User?.LastName}");
                Debug.WriteLine($"{p.User?.Email}");
            }

            var all = await _meetingService.GetAllUsersAsync();
            Debug.WriteLine($"[AllUsers] naèteno ze serveru: {all.Count}");
            Debug.WriteLine($"Recurrence: {meeting.Recurrence?.Pattern}, EndDate: {meeting.EndDate}");


            var existingIds = Participants.Select(p => p.Id).ToHashSet();
            AllUsers = new ObservableCollection<User>(all.Where(u => !existingIds.Contains(u.Id)));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        Debug.WriteLine($"[Search] hledám: {value}");
        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredUsers.Clear();
            return;
        }

        FilteredUsers = new ObservableCollection<User>(
            AllUsers.Where(u => u.FullName.Contains(value, StringComparison.OrdinalIgnoreCase))
        );
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

            var participant = new MeetingParticipant
            {
                MeetingId = MeetingId,
                UserId = user.Id
            };

            await _meetingService.AddParticipantAsync(MeetingId, participant);
        }
    }

    [RelayCommand]
    public async Task RemoveParticipantAsync(User user)
    {
        Debug.WriteLine($"Odebírám userId={user.Id} ze schùzky {meetingId}");
        await _meetingService.RemoveParticipantAsync(MeetingId, user.Id);
        Participants.Remove(user);
        
        if (!AllUsers.Any(u => u.Id == user.Id))
            AllUsers.Add(user);
    }

    public async Task LoadAsync()
    {
        if (MeetingId <= 0)
            return;

        var meeting = await _meetingService.GetMeetingByIdAsync(MeetingId);
        Debug.WriteLine($"Naèítám detail pro ID: {MeetingId}");
        Debug.WriteLine($"Naètený meeting: {meeting?.Title}");
        if (meeting != null)
        {
            SelectedMeeting = meeting;
            Participants = new ObservableCollection<Participant>(meeting.Participants);
        }
    }

    [RelayCommand]
    public async Task SaveChangesAsync()

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
