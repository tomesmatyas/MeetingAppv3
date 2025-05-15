using System.IdentityModel.Tokens.Jwt;

namespace MeetingApp.Services.Auth;

public class AuthGuardService
{
    private readonly ILocalStorageService _localStorage;

    public AuthGuardService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<bool> EnsureAuthenticatedAsync()
    {
        var token = await _localStorage.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            await RedirectToLogin();
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            await RedirectToLogin();
            return false;
        }

        var jwt = handler.ReadJwtToken(token);
        var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");

        if (expClaim == null || !long.TryParse(expClaim.Value, out long expUnix))
        {
            await RedirectToLogin();
            return false;
        }

        var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        if (expTime <= DateTimeOffset.UtcNow)
        {
            await RedirectToLogin();
            return false;
        }

        return true;
    }

    private static async Task RedirectToLogin()
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
