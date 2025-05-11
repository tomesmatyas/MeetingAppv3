using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MeetingApp.Models.ViewModels;

public partial class MeetingDetailViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;

    [ObservableProperty]
    private Meeting? _meeting;

    [ObservableProperty]
    private ObservableCollection<Participant> _participants = new();

    public MeetingDetailViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    [RelayCommand]
    public async Task LoadMeeting(int meetingId)
    {
        try
        {
            var meetings = await _meetingService.GetMeetingsAsync();
            Meeting = meetings?.FirstOrDefault(m => m.Id == meetingId);
            if (Meeting != null && Meeting.Participants != null)
            {
                Participants = new ObservableCollection<Participant>(Meeting.Participants);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi naèítání detailu: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task EditMeeting()
    {
        if (Meeting != null)
        {
            await Shell.Current.GoToAsync($"/EditMeetingPage?meetingId={Meeting.Id}");
        }
    }

    [RelayCommand]
    public async Task BackToCalendar()
    {
        await Shell.Current.GoToAsync("..", true);
    }
}
