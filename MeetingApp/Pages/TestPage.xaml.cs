using MeetingApp.Services;
using MeetingApp.ViewModels;

namespace MeetingApp.Pages;

public partial class TestPage : ContentPage
{
	public TestPage()
	{;
		InitializeComponent();
        BindingContext = new TestViewModel(new MeetingService(new HttpClient { BaseAddress = new Uri("http://localhost:5091") }));
    }
}