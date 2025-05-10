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

        int baseHour = 8;         // začátek dne (8:00)
        int blockMinutes = 30;    // 1 GridRow = 30 minut
        int baseMinutes = baseHour * 60;

        for (int i = 0; i < 7; i++)
        {
            var day = CurrentWeekStart.AddDays(i);
            var meetingsForDay = Meetings.Where(m => m.Date.Date == day.Date);
            var displays = new ObservableCollection<MeetingDisplay>();

            foreach (var meeting in meetingsForDay)
            {
                var start = meeting.StartTime;
                var end = meeting.EndTime;

                int gridRow = Math.Max(0, (int)((start.TotalMinutes - baseMinutes) / blockMinutes));
                int rowSpan = Math.Max(1, (int)((end - start).TotalMinutes / blockMinutes));

                displays.Add(new MeetingDisplay
                {
                    Meeting = meeting,
                    Title = meeting.Title,
                    GridRow = gridRow,
                    RowSpan = rowSpan,
                    ColorHex = string.IsNullOrEmpty(meeting.ColorHex) ? "#FF6600" : meeting.ColorHex
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
    public void OnPreviousWeekClicked()
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
