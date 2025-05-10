using MeetingApp.Models;
using MeetingApp.Models.ViewModels;
using System.Diagnostics;

namespace MeetingApp.Pages;

public partial class CalendarPage : ContentPage
{

    public CalendarPage(CalendarViewModel vm)
    {
        InitializeComponent();
        
        BindingContext = vm;
    }

    
    
}
