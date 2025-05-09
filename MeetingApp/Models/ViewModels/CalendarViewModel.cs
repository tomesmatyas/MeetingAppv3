// Models/ViewModels/CalendarViewModel.cs
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MeetingApp.Services;
using MeetingApp.Pages;
using System.Windows.Input;
using MeetingApp.Models;

namespace MeetingApp.Models.ViewModels
{
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

                var dayMeetings = Meetings
                    .Where(m => m.Date.Date == day.Date)
                    .Select(m => new MeetingDisplay
                    {
                        Meeting = m,
                        GridRow = (int)((m.StartTime.TotalMinutes - 480) / 30),
                        RowSpan = (int)Math.Ceiling((m.EndTime - m.StartTime).TotalMinutes / 30),
                        ColorHex = m.ColorHex ?? "#FF6600",
                        Title = m.Title ?? ""
                    });

                newDays.Add(new DayModel
                {
                    Date = day,
                    ColumnIndex = i + 1,
                    Meetings = new ObservableCollection<MeetingDisplay>(dayMeetings)
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
}
