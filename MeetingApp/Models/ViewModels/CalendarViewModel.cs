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
        var newDays = new ObservableCollection<DayModel>();
        Days.Clear();

        var allMeetings = Meetings.ToList();

        // Přidat instance opakujících se schůzek pro aktuální týden
        var recurringInstances = GenerateRecurringMeetingsForWeek(CurrentWeekStart);
        allMeetings.AddRange(recurringInstances);

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

    // Vygeneruj virtuální instance pro týden
    private List<Meeting> GenerateRecurringMeetingsForWeek(DateTime weekStart)
    {
        var result = new List<Meeting>();
        var weekEnd = weekStart.AddDays(7);

        foreach (var m in Meetings.Where(m => m.IsRegular && m.Recurrence != null))
        {
            var recurrence = m.Recurrence!;
            var pattern = recurrence.Pattern;
            var interval = recurrence.Interval;
            var endDate = m.EndDate ?? weekEnd;

            var originalDate = m.Date;

            DateTime dateIterator = recurrence.Pattern switch
            {
                "Weekly" => originalDate.StartOfWeek(DayOfWeek.Monday),
                "Monthly" => new DateTime(originalDate.Year, originalDate.Month, 1),
                _ => originalDate
            };

            while (dateIterator <= weekEnd)
            {
                if (dateIterator >= weekStart && dateIterator <= endDate)
                {
                    // ověřit, zda má být tento den instancí
                    if (pattern == "Weekly" && (dateIterator - originalDate).Days % (7 * interval) == 0)
                    {
                        var inst = new Meeting
                        {
                            Id = m.Id,
                            Title = m.Title,
                            Date = dateIterator,
                            StartTime = m.StartTime,
                            EndTime = m.EndTime,
                            ColorHex = m.ColorHex,
                            IsRegular = m.IsRegular,
                            RecurrenceId = m.RecurrenceId,
                            Recurrence = m.Recurrence,
                            Participants = m.Participants
                        };
                        result.Add(inst);
                    }
                    else if (pattern == "Monthly" && originalDate.Day == dateIterator.Day)
                    {
                        var monthsBetween = ((dateIterator.Year - originalDate.Year) * 12) + dateIterator.Month - originalDate.Month;
                        if (monthsBetween % interval == 0)
                        {
                            var inst = new Meeting
                            {
                                Id = m.Id,
                                Title = m.Title,
                                Date = dateIterator,
                                StartTime = m.StartTime,
                                EndTime = m.EndTime,
                                ColorHex = m.ColorHex,
                                IsRegular = m.IsRegular,
                                RecurrenceId = m.RecurrenceId,
                                Recurrence = m.Recurrence,
                                Participants = m.Participants
                            };
                            result.Add(inst);
                        }
                    }
                }
                dateIterator = dateIterator.AddDays(1);
            }
        }

        return result;
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
