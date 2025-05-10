using MeetingApp.Models;
using MeetingApp.Models.ViewModels;
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMeetings();
        if (_viewModel.Days.Count >= 7)
        {
            AddMeetingsToGrid(DayGrid0, _viewModel.Days[0]);
            AddMeetingsToGrid(DayGrid1, _viewModel.Days[1]);
            AddMeetingsToGrid(DayGrid2, _viewModel.Days[2]);
            AddMeetingsToGrid(DayGrid3, _viewModel.Days[3]);
            AddMeetingsToGrid(DayGrid4, _viewModel.Days[4]);
            AddMeetingsToGrid(DayGrid5, _viewModel.Days[5]);
            AddMeetingsToGrid(DayGrid6, _viewModel.Days[6]);
        }
    }
    private void AddMeetingsToGrid(Grid grid, DayModel day)
    {
        grid.Children.Clear();

        foreach (var meeting in day.Meetings)
        {
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

            var tap = new TapGestureRecognizer
            {
                //Command = _viewModel.EditMeetingCommand,
                //CommandParameter = meeting.Meeting
            };
            frame.GestureRecognizers.Add(tap);
            Debug.WriteLine();
            Grid.SetRow(frame, meeting.GridRow);
            Grid.SetRowSpan(frame, meeting.RowSpan);
            grid.Children.Add(frame);
        }
    }
}
