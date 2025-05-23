namespace MeetingApp.Services.Auth;

using MeetingApp.Models.Dtos;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsLoggedIn();
    UserDto? GetCurrentUser();
    Task<bool> RegisterAsync(string username, string password, string email, string firstName, string lastName);
}