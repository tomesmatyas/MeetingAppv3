using MeetingApp.Models.ViewModels;

namespace MeetingApp.Pages;

public partial class MeetingDetailPage : ContentPage
{
	public MeetingDetailPage(MeetingDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MeetingDetailViewModel vm)
            await vm.LoadAsync();
    }

}