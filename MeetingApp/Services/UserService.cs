using MeetingApp.Models.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeetingApp.Services.Auth;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Vrací seznam účastníků přihlášeného admina.
    /// </summary>
    public async Task<List<UserDto>> GetMyUsersAsync()
    {
        var users = await _httpClient.GetFromJsonAsync<List<UserDto>>("/api/meetings/my-users");
        return users ?? new List<UserDto>();
    }

    /// <summary>
    /// Přidá uživatele pod aktuálního admina.
    /// </summary>
    public async Task<bool> AddUserToAdminAsync(int userId)
    {
        var response = await _httpClient.PostAsync($"/api/meetings/add-user/{userId}", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Odebere uživatele od aktuálního admina.
    /// </summary>
    public async Task<bool> RemoveUserFromAdminAsync(int userId)
    {
        var response = await _httpClient.DeleteAsync($"/api/meetings/remove-user/{userId}");
        return response.IsSuccessStatusCode;
    }
}
