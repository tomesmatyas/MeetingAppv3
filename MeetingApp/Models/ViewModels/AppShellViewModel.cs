using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Services.Auth;
using System.Diagnostics;

namespace MeetingApp.Models.ViewModels;

public partial class AppShellViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    public UserSession Session { get; }
    public AppShellViewModel(IAuthService authService, UserSession session)
    {
        _authService = authService;
        Session = session;
        Debug.WriteLine($"[AppShellVM] Session.Username pøi konstrukci: {Session.Username}");
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert("Odhlásit se", "Opravdu se chcete odhlásit?", "Ano", "Ne");
        if (confirm)
        {
            Shell.Current.FlyoutIsPresented = false;
            await _authService.LogoutAsync();
        }
    }
}
