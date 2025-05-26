using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models;
using MeetingApp.Models.Dtos;
using MeetingApp.Pages;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MeetingApp.Models.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly MeetingService _meetingService;
    private readonly AuthGuardService _authGuard;

    [ObservableProperty] private bool isAdmin;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<MeetingDto> meetings = new();
    [ObservableProperty] private DateTime currentWeekStart = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
    [ObservableProperty] private ObservableCollection<DayModel> days = new();
    [ObservableProperty] private string weekRange = string.Empty;

    public CalendarViewModel(MeetingService meetingService, AuthGuardService authGuard)
    {
        _meetingService = meetingService;
        _authGuard = authGuard;
    }

    public async Task LoadMeetings()
    {
        try
        {
            var list = await _meetingService.GetMyMeetingsAsync();
            Meetings = new ObservableCollection<MeetingDto>(list);

            Debug.WriteLine("Načtené schůzky:");
            foreach (var meeting in Meetings)
                Debug.WriteLine($"  - {meeting.Title} na {meeting.Date:dd.MM.yyyy}");

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
        foreach (var day in Days)
            day.Meetings.Clear();
        Days.Clear();

        var allMeetings = Meetings.ToList();
        var recurringInstances = GenerateRecurringMeetingsForWeek(CurrentWeekStart);

        allMeetings.AddRange(recurringInstances);

        int baseHour = 8;
        int blockMinutes = 30;
        int baseMinutes = baseHour * 60;

        for (int i = 0; i < 7; i++)
        {
            var day = CurrentWeekStart.AddDays(i);
            var displays = new ObservableCollection<MeetingDisplay>();

            var dailyMeetings = allMeetings.Where(m => m.Date.Date == day.Date).ToList();
            var meetingDisplays = new List<MeetingDisplay>();

            foreach (var meeting in dailyMeetings)
            {
                int gridRow = Math.Max(0, (int)((meeting.StartTime.TotalMinutes - baseMinutes) / blockMinutes));
                int rowSpan = Math.Max(1, (int)((meeting.EndTime - meeting.StartTime).TotalMinutes / blockMinutes));

                meetingDisplays.Add(new MeetingDisplay
                {
                    Meeting = meeting,
                    GridRow = gridRow,
                    RowSpan = rowSpan
                });
            }

            var assigned = new HashSet<MeetingDisplay>();

            foreach (var meeting in meetingDisplays)
            {
                if (assigned.Contains(meeting)) continue;

                var overlapping = meetingDisplays.Where(m =>
                    (m.StartTime < meeting.EndTime && m.EndTime > meeting.StartTime)).ToList();

                for (int k = 0; k < overlapping.Count; k++)
                {
                    overlapping[k].Column = k;
                    overlapping[k].TotalColumns = overlapping.Count;
                    assigned.Add(overlapping[k]);
                }
            }

            foreach (var d in meetingDisplays)
                displays.Add(d);

            newDays.Add(new DayModel
            {
                Date = day,
                ColumnIndex = i + 1,
                Meetings = displays
            });
        }

        Days = newDays;
        WeekRange = $"{CurrentWeekStart:dd. MM.} - {CurrentWeekStart.AddDays(6):dd. MM. yyyy}";
        WeakReferenceMessenger.Default.Send(new RefreshCalendarMessage());
    }

    private List<MeetingDto> GenerateRecurringMeetingsForWeek(DateTime weekStart)
    {
        var result = new List<MeetingDto>();
        var weekEnd = weekStart.AddDays(7);

        var existingKeys = new HashSet<string>(
            Meetings.Select(m => $"{m.Title}|{m.Date.Date}|{m.StartTime}-{m.EndTime}")
        );

        foreach (var m in Meetings.Where(m => m.IsRegular && !string.IsNullOrEmpty(m.RecurrencePattern)))
        {
            string pattern = m.RecurrencePattern ?? "Ne";
            var originalDate = m.Date;
            var endDate = m.EndDate ?? weekEnd;
            int interval = m.Interval;

            DateTime dateIterator = pattern switch
            {
                "Týden" => originalDate.StartOfWeek(DayOfWeek.Monday),
                "Měsíc" => new DateTime(originalDate.Year, originalDate.Month, 1, 0, 0, 0),
                _ => DateTime.MinValue
            };

            while (dateIterator <= weekEnd)
            {
                if (dateIterator >= weekStart && dateIterator <= endDate)
                {
                    bool isValid =
                        (pattern == "Týden" && (dateIterator - originalDate).Days % (7 * interval) == 0) ||
                        (pattern == "Měsíc" && originalDate.Day == dateIterator.Day &&
                         ((dateIterator.Year - originalDate.Year) * 12 + dateIterator.Month - originalDate.Month) % interval == 0);

                    if (isValid)
                    {
                        var key = $"{m.Title}|{dateIterator.Date}|{m.StartTime}-{m.EndTime}";

                        if (!existingKeys.Contains(key))
                        {
                            existingKeys.Add(key);
                            result.Add(new MeetingDto
                            {
                                Id = m.Id,
                                Title = m.Title,
                                Date = dateIterator,
                                StartTime = m.StartTime,
                                EndTime = m.EndTime,
                                ColorHex = m.ColorHex,
                                IsRegular = m.IsRegular,
                                RecurrenceId = m.RecurrenceId,
                                RecurrencePattern = pattern,
                                Interval = m.Interval,
                                Participants = m.Participants,
                                CreatedByUserId = m.CreatedByUserId,
                                
                                EndDate = m.EndDate
                            });
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
}

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}
