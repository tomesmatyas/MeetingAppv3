using CommunityToolkit.Mvvm.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace MeetingApp.Services.Auth;

public partial class UserSession : ObservableObject
{
    private static readonly Lazy<UserSession> _instance = new(() => new UserSession());
    public static UserSession Instance => _instance.Value;

    public static bool IsAdminCheck => Instance.IsAdmin;

    [ObservableProperty] private bool isAdmin;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string? username;
    [ObservableProperty] private int? userId;
    [ObservableProperty] private string? firstName;
    [ObservableProperty] private string? lastName;

    public string FullName =>
    string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Username ?? ""
        : $"{FirstName} {LastName}".Trim();
    private UserSession() { }


    public async Task LoadAsync()
    {
        var token = await SecureStorage.Default.GetAsync("access_token");
        Debug.WriteLine($"[SESSION] Token načten: {(token is null ? "null" : "OK")}");

        if (string.IsNullOrEmpty(token))
        {
            Debug.WriteLine("[SESSION] Token je prázdný – volám Clear()");
            Clear();
            return;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Username = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        UserId = int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int id) ? id : null;
        IsAdmin = string.Equals(jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value, "Admin", StringComparison.OrdinalIgnoreCase);
        IsAuthenticated = true;

        FirstName = jwt.Claims.FirstOrDefault(c => c.Type == "firstName")?.Value;
        LastName = jwt.Claims.FirstOrDefault(c => c.Type == "lastName")?.Value;
        
        
        

        Debug.WriteLine($"[SESSION] Username: {Username}, IsAdmin: {IsAdmin}");
    }

    public void Clear()
    {
        IsAdmin = false;
        IsAuthenticated = false;
        Username = null;
        UserId = null;
    }
}
