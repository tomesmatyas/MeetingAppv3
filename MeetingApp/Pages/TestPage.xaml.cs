using MeetingApp.Services;
using MeetingApp.ViewModels;

namespace MeetingApp.Pages;

public partial class TestPage : ContentPage
{
	public TestPage(TestViewModel vm)
	{;
		InitializeComponent();
		BindingContext = vm;

    }
}