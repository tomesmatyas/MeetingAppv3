using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Services.Auth;

namespace MeetingApp.Models.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    public AppShellViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert("Odhl�sit se", "Opravdu se chcete odhl�sit?", "Ano", "Ne");
        if (confirm)
        {
            await _authService.LogoutAsync();
        }
    }
}
