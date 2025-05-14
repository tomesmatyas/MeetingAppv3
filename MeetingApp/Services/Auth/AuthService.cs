using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using MeetingApp.Models.Dtos;
using MeetingApp.Services.Auth;

namespace MeetingApp.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private UserDto? _user;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });

        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result == null || string.IsNullOrEmpty(result.Token)) return false;

        Preferences.Set("access_token", result.Token);
        Preferences.Set("user_id", result.User.Id);
        Preferences.Set("user_name", result.User.FirstName);

        _user = result.User;
        return true;
    }

    public async Task LogoutAsync()
    {
        Preferences.Remove("access_token");
        Preferences.Remove("user_id");
        Preferences.Remove("user_name");

        _user = null;
        await Shell.Current.GoToAsync("//LoginPage", true);
    }

    public bool IsLoggedIn() =>
        !string.IsNullOrEmpty(Preferences.Get("access_token", string.Empty));

    public UserDto? GetCurrentUser() => _user;
}
