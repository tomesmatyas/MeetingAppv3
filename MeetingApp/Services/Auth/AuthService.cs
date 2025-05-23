using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using MeetingApp.Models.Dtos;
using MeetingApp.Services.Auth;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace MeetingApp.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private UserDto? _user;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        Debug.WriteLine(" Pokouším se přihlásit...");

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });

        Debug.WriteLine($" Odpověď z API: {(int)response.StatusCode} {response.ReasonPhrase}");

        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine(" Přihlášení selhalo (chybný status).");
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        if (result == null || string.IsNullOrEmpty(result.Token))
        {
            Debug.WriteLine(" Token je null nebo prázdný.");
            return false;
        }

        Debug.WriteLine($" Token přijat: {result.Token.Substring(0, 20)}...");

        // ⬇️ ZMĚNA: ukládání do SecureStorage
        await SecureStorage.Default.SetAsync("access_token", result.Token);
        await SecureStorage.Default.SetAsync("user_id", result.User.Id.ToString());
        await SecureStorage.Default.SetAsync("user_name", result.User.FirstName);

        await _localStorage.SaveTokenAsync(result.Token);
        Debug.WriteLine(" Token uložen do local storage.");

        await UserSession.Instance.LoadAsync();
        Debug.WriteLine($"[SESSION] Session.Username: {UserSession.Instance.Username}");

        var token = await _localStorage.GetTokenAsync();
        Debug.WriteLine($"TOKEN >>> {token}");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        _user = result.User;
        Debug.WriteLine($" Přihlášený uživatel: {result.User.FirstName} (ID: {result.User.Id})");

        return true;
    }


    public async Task LogoutAsync()
    {
        SecureStorage.Default.Remove("access_token");
        SecureStorage.Default.Remove("user_id");
        SecureStorage.Default.Remove("user_name");

        _user = null;
        await Shell.Current.GoToAsync("//LoginPage", true);
    }

    public async Task<bool> RegisterAsync(string username, string password, string email, string firstName, string lastName)
    {
        var data = new
        {
            username,
            passwordHash = password, // pokud je potřeba
            email,
            firstName,
            lastName
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/register", data);

        return response.IsSuccessStatusCode;
    }


    public bool IsLoggedIn() =>
        !string.IsNullOrEmpty(Preferences.Get("access_token", string.Empty));

    public UserDto? GetCurrentUser() => _user;
}
