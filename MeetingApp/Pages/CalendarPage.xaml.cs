using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models;
using MeetingApp.Models.ViewModels;
using MeetingApp.Services;
using System.Diagnostics;

namespace MeetingApp.Pages;

public partial class CalendarPage : ContentPage
{
    private readonly CalendarViewModel _viewModel;
    public CalendarPage(CalendarViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
        WeakReferenceMessenger.Default.Register<RefreshCalendarMessage>(this, (r, m) =>
        {
            RefreshCalendar();
        });
    }
    private void RefreshCalendar()
    {
        List<Grid> dayGrids = new() { DayGrid0, DayGrid1, DayGrid2, DayGrid3, DayGrid4, DayGrid5, DayGrid6 };

        for (int i = 0; i < 7; i++)
        {
            dayGrids[i].Children.Clear();
            AddMeetingsToGrid(dayGrids[i], _viewModel.Days[i]);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadMeetings();
        Debug.WriteLine($"Poèet dnù: {_viewModel.Days.Count}");
        for (int i = 0; i < _viewModel.Days.Count; i++)
        {
            Debug.WriteLine($"Den {i}: {_viewModel.Days[i].Date:dd.MM.yyyy}, schùzek: {_viewModel.Days[i].Meetings.Count}");
        }
        List<Grid> dayGrids = new() { DayGrid0, DayGrid1, DayGrid2, DayGrid3, DayGrid4, DayGrid5, DayGrid6 };

        foreach (var grid in dayGrids)
            grid.Children.Clear();

        for (int i = 0; i < 7; i++)
            AddMeetingsToGrid(dayGrids[i], _viewModel.Days[i]);
    }
    private void AddMeetingsToGrid(Grid grid, DayModel day)
    {
        grid.Children.Clear();
        Debug.WriteLine($" Renderuji den: {day.Date:dd.MM.yyyy} ({day.Meetings.Count} schùzek)");

        foreach (var meeting in day.Meetings)
        {
            Debug.WriteLine($" {meeting.Title} {meeting.TimeRange}");
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb(meeting.ColorHex),
                CornerRadius = 5,
                Padding = 4,
                Margin = new Thickness(1),
                Content = new VerticalStackLayout
                {
                    Children =
                    {
                        new Label { Text = meeting.Title, FontSize = 12, TextColor = Colors.White },
                        new Label { Text = meeting.TimeRange, FontSize = 10, TextColor = Colors.White },
                        new Label { Text = meeting.ParticipantInfo, FontSize = 10, TextColor = Colors.White }
                    }
                }

            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {

                await Shell.Current.GoToAsync($"{nameof(MeetingDetailPage)}?id={meeting.Id}");
            };
            frame.GestureRecognizers.Add(tap);

            Grid.SetRow(frame, meeting.GridRow);
            Grid.SetRowSpan(frame, meeting.RowSpan);
            grid.Children.Add(frame);
        }
    }
}
