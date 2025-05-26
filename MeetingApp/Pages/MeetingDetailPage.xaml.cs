using MeetingApp.Models.ViewModels;
using MeetingApp.Services.Auth;
using Microsoft.Maui.Platform;
using Syncfusion.Maui.Core.Carousel;

namespace MeetingApp.Pages;

public partial class MeetingDetailPage : ContentPage
{
	public MeetingDetailPage(MeetingDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

	}
    private ToolbarItem _saveItem;
    private ToolbarItem _deleteItem;
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MeetingDetailViewModel vm)
        {
            await vm.LoadAsync();

            ToolbarItems.Clear();

            if (UserSession.IsAdminCheck)
            {
                _saveItem = new ToolbarItem
                {
                    Text = "Uložit",
                    Command = vm.SaveChangesButtonCommand
                };
                ToolbarItems.Add(_saveItem);

                _deleteItem = new ToolbarItem
                {
                    Text = "Smazat",
                    Order = ToolbarItemOrder.Secondary,
                    Priority = 1,
                    Command = vm.DeleteMeetingCommand
                };
                ToolbarItems.Add(_deleteItem);
            }
        }


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