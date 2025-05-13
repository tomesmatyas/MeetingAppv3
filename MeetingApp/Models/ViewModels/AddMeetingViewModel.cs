using MeetingApp.Models;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MeetingApp.ViewModels;

public partial class AddMeetingViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;
    public AddMeetingViewModel() { }
    public AddMeetingViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
        Participants = new ObservableCollection<MeetingParticipant>();
        SelectedDate = DateTime.Today;
        SelectedTime = TimeOnly.FromDateTime(DateTime.Now);
    }

    [ObservableProperty]
    string title;

    [ObservableProperty]
    DateTime selectedDate;

    [ObservableProperty]
    TimeOnly selectedTime;

    [ObservableProperty]
    bool isRegular;

    public ObservableCollection<MeetingParticipant> Participants { get; }

    [RelayCommand]
    void AddParticipant()
    {
        Participants.Add(new MeetingParticipant());
    }

    [RelayCommand]
    async Task AddMeeting()
    {
        var dateTime = SelectedDate.Date + SelectedTime.ToTimeSpan();

        var meeting = new Meeting
        {
            Title = Title,
            Date = dateTime,
            IsRegular = IsRegular,
            Participants = Participants.ToList()
        };

        var success = await _meetingService.AddMeetingAsync(meeting);

        if (success)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Chyba", "Nepodaøilo se uložit schùzku.", "OK");
        }
    }

    [RelayCommand]
    async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
