
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    }

    public async Task LoadMeetings()
    {
        try
        {
            var meetings = await _meetingService.GetMeetingsAsync();
            if (meetings != null)
            {
                Meetings.Clear();
                Debug.WriteLine("Načtené schůzky:");
                foreach (var meeting in meetings)
                {
                    Debug.WriteLine($"  - {meeting.Title} na {meeting.Date:dd.MM.yyyy}");
                    Meetings.Add(meeting);
                }
            }
            UpdateCalendar();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba při načítání schůzek: {ex.Message}");
        }
    }

    private void UpdateCalendar()
    {
        Days.Clear();
        var allMeetings = Meetings.ToList();
        var recurringInstances = GenerateRecurringMeetingsForWeek(CurrentWeekStart);
        allMeetings.AddRange(recurringInstances);

        var newDays = new ObservableCollection<DayModel>();
        int baseHour = 8;
        int blockMinutes = 30;
        int baseMinutes = baseHour * 60;

        for (int i = 0; i < 7; i++)
        {
            var day = CurrentWeekStart.AddDays(i);
            var meetingsForDay = allMeetings.Where(m => m.Date.Date == day.Date);
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
        WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
    }

    private List<Meeting> GenerateRecurringMeetingsForWeek(DateTime weekStart)
    {
        var result = new List<Meeting>();
        var weekEnd = weekStart.AddDays(6);

        foreach (var m in Meetings.Where(m => m.IsRegular && m.Recurrence != null))
        {
            var recurrence = m.Recurrence!;
            var interval = recurrence.Interval;
            var endDate = m.EndDate ?? weekEnd;
            var originalDate = m.Date;

            DateTime iterator = weekStart;

            while (iterator <= weekEnd && iterator <= endDate)
            {
                if (recurrence.Pattern == "Weekly" &&
                    (iterator - originalDate).Days % (7 * interval) == 0)
                {
                    result.Add(CloneRecurringMeeting(m, iterator));
                }
                else if (recurrence.Pattern == "Monthly" &&
                         originalDate.Day == iterator.Day)
                {
                    var monthsBetween = ((iterator.Year - originalDate.Year) * 12) + iterator.Month - originalDate.Month;
                    if (monthsBetween % interval == 0)
                    {
                        result.Add(CloneRecurringMeeting(m, iterator));
                    }
                }

                iterator = iterator.AddDays(1);
            }
        }

        return result;
    }

    private Meeting CloneRecurringMeeting(Meeting source, DateTime newDate)
    {
        return new Meeting
        {
            Id = source.Id,
            Title = source.Title,
            Date = newDate,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            ColorHex = source.ColorHex,
            IsRegular = true,
            RecurrenceId = source.RecurrenceId,
            Recurrence = source.Recurrence,
            Participants = source.Participants,
            CreatedByUserId = source.CreatedByUserId
        };
    }

    [RelayCommand]
    public void OnPreviousWeekClicked()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(-7);
        UpdateCalendar();
        WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
    }

    [RelayCommand]
    public void OnNextWeekClicked()
    {
        CurrentWeekStart = CurrentWeekStart.AddDays(7);
        UpdateCalendar();
        WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
    }

    [RelayCommand]
    public async Task OnAddMeetingClicked()
    {
        await LoadMeetings();
        await Shell.Current.GoToAsync(nameof(AddMeetingPage));
    }

    [RelayCommand]
    public async Task EditMeeting()
    {
        await LoadMeetings();
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
