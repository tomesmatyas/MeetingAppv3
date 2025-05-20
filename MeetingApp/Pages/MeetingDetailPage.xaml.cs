using MeetingApp.Models.ViewModels;
using Microsoft.Maui.Platform;

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
    private void OnPageTapped(object sender, TappedEventArgs e)
    {
        SearchBarControl?.Unfocus(); // ⬅ klávesnice zmizí
    }



    void OnSearchBarFocused(object sender, FocusEventArgs e)
    {
        MainScrollView.ScrollToAsync(0, 0, true);
    }

}