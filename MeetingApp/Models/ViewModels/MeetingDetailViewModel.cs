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

    [ObservableProperty]
    private int meetingId;

    [ObservableProperty]
    private Meeting? selectedMeeting;

    [ObservableProperty]
    private ObservableCollection<User> participants = new();

    [ObservableProperty]
    private string? title;

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private TimeSpan startTime;

    [ObservableProperty]
    private TimeSpan endTime;

    [ObservableProperty]
    private string pattern = "None";

    [ObservableProperty]
    private DateTime? endDate;

    [ObservableProperty]
    private string fullname = "None";

    [ObservableProperty]
    private ObservableCollection<User> allUsers = new();

    [ObservableProperty]
    private User? selectedUserToAdd;

    [ObservableProperty]
    private string searchText;

    [ObservableProperty]
    private ObservableCollection<User> filteredUsers = new();

    public List<string> RecurrencePatterns => new() { "None", "Weekly", "Monthly" };

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

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        if (SelectedMeeting == null) return;

        // Zajištìní, že Recurrence bude správnì nastaveno
        if (Pattern == "None")
        {
            SelectedMeeting.IsRegular = false;
            SelectedMeeting.Recurrence = null;
            SelectedMeeting.EndDate = DateTime.Today;  // Pro jednoroèní schùzku nastavíme EndDate na dnešní datum
        }
        else
        {
            SelectedMeeting.IsRegular = true;

            if (SelectedMeeting.Recurrence == null)
                SelectedMeeting.Recurrence = new MeetingRecurrence();

            SelectedMeeting.Recurrence.Pattern = Pattern;
            SelectedMeeting.Recurrence.Interval = 1; // fixnì nebo na vstup
            SelectedMeeting.EndDate = EndDate;  // Nastavíme EndDate pro Meeting
        }

        // Logování pøed odesláním
        var meetingJson = JsonSerializer.Serialize(SelectedMeeting, new JsonSerializerOptions
        {
            WriteIndented = true, // Formátování JSON pro lepší èitelnost
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Použití CamelCase pro serializaci
            ReferenceHandler = ReferenceHandler.Preserve // Ošetøení cyklických závislostí
        });

        Debug.WriteLine("Odesílám Meeting objekt na server:");
        Debug.WriteLine(meetingJson); // Zobrazí serializovaný objekt v konzoli pro kontrolu

        // Odeslání dat na server
        var success = await _meetingService.UpdateMeetingAsync(SelectedMeeting);
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
            Debug.WriteLine($"[Save] Pattern: {Pattern}, EndDate: {EndDate}");
            Debug.WriteLine("Schùzka byla aktualizována.");
        }
    }

    [ObservableProperty]
    private bool showEndDate = false;

    partial void OnPatternChanged(string value)
    {
        if (value == "None")
        {
            EndDate = DateTime.Today; // Pokud není opakování, nastavíme EndDate na dnešní datum
        }
        else
        {
            var today = DateTime.Today;
            EndDate = value == "Weekly"
                ? today.AddMonths(1)  // Pro Weekly nastavíme EndDate na mìsíc
                : today.AddMonths(2); // Pro Monthly nastavíme EndDate na dva mìsíce

            // Ochrana proti dnešnímu datu
            if (EndDate <= today)
            {
                Shell.Current.DisplayAlert("Neplatné datum", "Zadejte datum v budoucnosti.", "OK");
                EndDate = today.AddDays(7); // Nouzová hodnota
            }
        }

        // Nyní budete mít EndDate správnì nastaveno pro schùzky s patternem "Weekly" nebo "Monthly"
        SaveChangesAsync(); // Save zmìny až po zmìnì patternu
        Debug.WriteLine("Zmìny patternu uloženy");
    }

    partial void OnEndDateChanged(DateTime? value)
    {
        Debug.WriteLine("Vybral jsem datum");
        SaveChangesAsync();
    }

    private void ValidateEndDate(DateTime selected)
    {
        if (selected <= DateTime.Today)
        {
            Shell.Current.DisplayAlert("Chyba", "Datum, do kdy se má schùzka, musí být pozdìji než dnešní!", "OK");
   
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
