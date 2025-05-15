using MeetingApp.Models.Dtos;
using MeetingApp.Models.ViewModels;


namespace MeetingApp.Pages;

public partial class AddMeetingPage : ContentPage
{
	public AddMeetingPage(AddMeetingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
    
}