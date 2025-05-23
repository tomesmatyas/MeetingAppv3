using MeetingApp.Models.ViewModels;

namespace MeetingApp.Pages;

public partial class RegistrationPage : ContentPage
{
	public RegistrationPage(RegistrationViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}