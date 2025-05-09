using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MeetingApp.Services;
using MeetingApp.Pages;



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
        private ObservableCollection<DayViewModel> _days = new();

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
            var newDays = new ObservableCollection<DayViewModel>();

            Days.Clear();
            for (int i = 0; i < 7; i++)
            {
                var day = CurrentWeekStart.AddDays(i);
                var dayViewModel = new DayViewModel
                {
                    Date = day,
                    Meetings = new ObservableCollection<Meeting>(Meetings.Where(m => m.Date.Date == day.Date))
                };
                newDays.Add(dayViewModel);
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
            // Navigace na stránku AddMeetingPage
            await Shell.Current.GoToAsync(nameof(AddMeetingPage));
        }


        
    }


    //public class DayViewModel
    //{
    //    public DateTime Date { get; set; }
    //    public ObservableCollection<Meeting> Meetings { get; set; } = new();
    //}

    //public static class DateTimeExtensions
    //{
    //    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    //    {
    //        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
    //        return dt.AddDays(-1 * diff).Date;
    //    }
    //}
}
