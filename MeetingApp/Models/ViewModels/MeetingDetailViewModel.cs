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
    private ObservableCollection<Participant> participants = new();

    public MeetingDetailViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
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
    {
        if (SelectedMeeting != null)
        {
            var success = await _meetingService.UpdateMeetingAsync(SelectedMeeting);
            if (success)
            {
                WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
                Debug.WriteLine("Schùzka byla aktualizována.");
            }
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
}
