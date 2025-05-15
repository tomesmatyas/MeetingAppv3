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
        Debug.WriteLine($"[AppShellVM] Session.Username p�i konstrukci: {Session.Username}");
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert("Odhl�sit se", "Opravdu se chcete odhl�sit?", "Ano", "Ne");
        if (confirm)
        {
            Shell.Current.FlyoutIsPresented = false;
            await _authService.LogoutAsync();
        }
    }
}
