using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingApp.Services.Auth;
using Xunit;
using Xunit.Sdk;

namespace MeetingApp.Models.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;

    [ObservableProperty] private string errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public RegistrationViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            var success = await _authService.RegisterAsync(Username, Password, Email, FirstName, LastName);
            if (success)
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                ErrorMessage = "Registrace selhala.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}