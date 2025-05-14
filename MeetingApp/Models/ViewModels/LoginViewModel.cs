using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using MeetingApp.Models.Dtos;
using MeetingApp.Services;
using System.Diagnostics;
using Microsoft.Maui.Storage;
using MeetingApp.Services.Auth;

namespace MeetingApp.Models.ViewModels;

public partial class LoginViewModel : ObservableObject
{
   
    private readonly IAuthService _authService;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        var success = await _authService.LoginAsync(Username, Password);
        if (success)
        {
            await Shell.Current.GoToAsync("//CalendarPage");
        }
        else
        {
            ErrorMessage = "Neplatné pøihlašovací údaje.";
        }
    }
}