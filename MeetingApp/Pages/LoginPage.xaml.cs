using MeetingApp.Models.ViewModels;
using MeetingApp.Services.Auth;

namespace MeetingApp.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}