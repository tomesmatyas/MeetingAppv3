using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Models;
using MeetingApp.Pages;
using MeetingApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MeetingApp.Models.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Meeting> _meetings = new();

    [ObservableProperty]
    private DateTime _currentWeekStart = DateTime.Today.StartOfWeek(DayOfWeek.Monday);

    [ObservableProperty]
    private ObservableCollection<DayModel> _days = new();

    [ObservableProperty]
    private string _weekRange = string.Empty;

    public CalendarViewModel(MeetingService meetingService)
    {
        _meetingService = meetingService;
        LoadMeetings();
        UpdateCalendar();
    }

    private async void LoadMeetings()
    {
        try
        {
            var meetings = await _meetingService.GetMeetingsAsync();
            if (meetings != null)
            {
                Meetings.Clear();
                foreach (var meeting in meetings)
                {
                    Meetings.Add(meeting);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba při načítání schůzek: {ex.Message}");
        }
    }

    private void UpdateCalendar()
    {
        var newDays = new ObservableCollection<DayModel>();
        Days.Clear();

        for (int i = 0; i < 7; i++)
        {
            var day = CurrentWeekStart.AddDays(i);
            var meetingsForDay = Meetings.Where(m => m.Date.Date == day.Date);
            var displays = new ObservableCollection<MeetingDisplay>();

            foreach (var meeting in meetingsForDay)
            {
                var start = meeting.StartTime;
                var end = meeting.EndTime;

                int baseHour = 8;
                int rowsPerHour = 2;

                int gridRow = Math.Max(0, (int)((start.TotalHours - baseHour) * rowsPerHour));
                int rowSpan = Math.Max(1, (int)((end - start).TotalHours * rowsPerHour));

                displays.Add(new MeetingDisplay
                {
                    Meeting = meeting,
                    Title = meeting.Title,
                    GridRow = gridRow,
                    RowSpan = rowSpan,
                    ColorHex = string.IsNullOrEmpty(meeting.ColorHex) ? "#FF6600" : meeting.ColorHex,
                    TimeRange = $"{start:hh\\:mm}–{end:hh\\:mm}",
                    ParticipantInfo = meeting.Participants?.Count.ToString() ?? "0"
                });
            }

            newDays.Add(new DayModel
            {
                Date = day,
                ColumnIndex = i + 1,
                Meetings = displays
            });
        }

        Days = newDays;
        WeekRange = $"{CurrentWeekStart:dd.} - {CurrentWeekStart.AddDays(6):dd. MM. yyyy}";
    }

    [RelayCommand]
    private void OnPreviousWeekClicked()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(-7);
        UpdateCalendar();
    }

    [RelayCommand]
    public void OnNextWeekClicked()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(7);
        UpdateCalendar();
    }

    [RelayCommand]
    public async Task OnAddMeetingClicked()
    {
        await Shell.Current.GoToAsync(nameof(AddMeetingPage));
    }
}

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
