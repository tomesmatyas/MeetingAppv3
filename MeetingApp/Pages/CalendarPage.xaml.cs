// File: CalendarPage.xaml.cs (p�id�no resetov�n� Days p�ed generac� nov�ch dat)
using CommunityToolkit.Mvvm.Messaging;
using MeetingApp.Models;
using MeetingApp.Models.ViewModels;
using MeetingApp.Services;
using System.Diagnostics;

namespace MeetingApp.Pages;

public partial class CalendarPage : ContentPage
{
    private readonly CalendarViewModel _viewModel;
    private bool _initialized = false;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMeetings();
    }

    private void RefreshCalendar()
    {
        List<Grid> layouts = new() { DayGrid0, DayGrid1, DayGrid2, DayGrid3, DayGrid4, DayGrid5, DayGrid6 };

        for (int i = 0; i < 7; i++)
        {
            var dayGrid = layouts[i];
            dayGrid.Children.Clear();
            dayGrid.ColumnDefinitions.Clear();

            int maxCols = _viewModel.Days[i].Meetings.Any() ? _viewModel.Days[i].Meetings.Max(m => m.TotalColumns) : 1;
            for (int c = 0; c < maxCols; c++)
                dayGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int maxRows = dayGrid.RowDefinitions.Count > 0 ? dayGrid.RowDefinitions.Count : 29;
            bool isToday = _viewModel.Days[i].Date.Date == DateTime.Today.Date;

            for (int row = 0; row < maxRows; row++)
            {
                BoxView bg;

                if (isToday)
                {
                    bg = new BoxView
                    {
                        Color = Colors.HotPink, // nebo zvol svou růžovou #FF69B4
                        Opacity = 0.1
                    };
                }
                else if (i == 1 || i == 3 || i == 5) // Úterý, Pátek, Sobota
                {
                    bg = new BoxView
                    {
                        Color = Colors.LightBlue,
                        Opacity = 0.5
                    };
                }
                else
                {
                    continue; // pro ostatní dny nepřidávej pozadí
                }

                Grid.SetRow(bg, row);
                Grid.SetColumn(bg, 0);
                Grid.SetColumnSpan(bg, dayGrid.ColumnDefinitions.Count);
                dayGrid.Children.Add(bg);
            }
            AddMeetingsToGrid(dayGrid, _viewModel.Days[i]);
        }
    }

    private void AddMeetingsToGrid(Grid grid, DayModel day)
    {
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
                        new Label { Text = meeting.Title, FontSize = 5, TextColor = Colors.White },
                        new Label { Text = meeting.TimeRange, FontSize = 5, TextColor = Colors.White },
                        new Label { Text = meeting.ParticipantInfo, FontSize = 5, TextColor = Colors.White }
                    }
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await Shell.Current.GoToAsync($"{nameof(MeetingDetailPage)}?id={meeting.Id}");
            };
            frame.GestureRecognizers.Add(tap);

            Grid.SetColumn(frame, meeting.Column);
            Grid.SetColumnSpan(frame, 1);
            Grid.SetRow(frame, meeting.GridRow);
            Grid.SetRowSpan(frame, meeting.RowSpan);

            grid.Children.Add(frame);
        }
    }
}