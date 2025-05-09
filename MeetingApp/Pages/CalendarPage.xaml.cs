using MeetingApp.Models.ViewModels;

namespace MeetingApp.Pages;

public partial class CalendarPage : ContentPage
{
    public CalendarPage(CalendarViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        
    }
}