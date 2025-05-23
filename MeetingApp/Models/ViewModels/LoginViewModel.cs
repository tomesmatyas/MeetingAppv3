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
    private readonly MeetingService _meetingService;
    private readonly IAuthService _authService;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;

    public LoginViewModel(IAuthService authService, MeetingService meetingService)
    {
        _authService = authService;
        _meetingService = meetingService;
    }

    [ObservableProperty]
    private string errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError)); // aktualizuje bindi na IsVisible
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("//RegistrationPage");
    }


    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var success = await _authService.LoginAsync(Username, Password);

            if (success)
            {
                await _meetingService.InitAsync(); // <== DÙLEŽITÉ
                await Shell.Current.GoToAsync("//CalendarPage");
            }
            else
            {
                ErrorMessage = "Neplatné pøihlašovací údaje.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Nepodaøilo se pøipojit k serveru: {ex.Message}";
            Debug.WriteLine($".-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.");
            Debug.WriteLine($" HttpRequestException ? {ex}");
            Debug.WriteLine($".-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Chyba: " + ex.Message;
        }
    }

}