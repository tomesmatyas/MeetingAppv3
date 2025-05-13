
using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using Newtonsoft.Json;
using MeetingApp.Services;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MeetingApp.Models.Dtos;

namespace MeetingApp.Services;

public class MeetingService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public MeetingService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _httpClient.BaseAddress = new Uri("http://localhost:5091");
    }

    public async Task<List<MeetingDto>> GetMeetingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/meetings");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var meetings = JsonConvert.DeserializeObject<List<MeetingDto>>(json);
                return meetings ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba: {ex.Message}");
        }

        return new();
    }

    public async Task<MeetingDto?> GetMeetingByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/meetings/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MeetingDto>(json);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Detail chyba: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> AddMeetingAsync(MeetingDto meeting)
    {
        try
        {
            if (meeting.IsRegular && meeting.Recurrence != null && meeting.EndDate == null)
                meeting.EndDate = DateTime.Now.AddMonths(6);

            var response = await _httpClient.PostAsJsonAsync("/api/meetings", meeting);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> UpdateMeetingAsync(MeetingDto meeting)
    {
        try
        {
            if (meeting.IsRegular && meeting.Recurrence != null && meeting.EndDate == null)
                meeting.EndDate = DateTime.Now.AddMonths(6);

            var response = await _httpClient.PutAsJsonAsync($"/api/meetings/{meeting.Id}", meeting);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Chyba] UpdateMeetingAsync: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> DeleteMeetingAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/meetings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Chyba] DeleteMeeting: {ex.Message}");
            return false;
        }
    }

    public async Task AddParticipantAsync(int meetingId, int userId)
    {
        try
        {
            var payload = new { userId = userId };
            await _httpClient.PostAsJsonAsync($"/api/meetings/{meetingId}/participants", payload);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Chyba] AddParticipantAsync: {ex.Message}");
        }
    }

    public async Task RemoveParticipantAsync(int meetingId, int userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/meetings/{meetingId}/users/{userId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Chyba] RemoveParticipantAsync: {ex.Message}");
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/users");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba při načítání uživatelů: {ex.Message}");
        }

        return new();
    }
    public async Task SyncPendingChangesAsync()
    {
        if (!Connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet))
            return;

        var pendingMeetings = await _localStorage.LoadPendingMeetingsAsync();
        foreach (var meeting in pendingMeetings.ToList())
        {
            try
            {
                HttpResponseMessage response;
                if (meeting.Id == 0)
                    response = await _httpClient.PostAsJsonAsync("/api/meetings", meeting);
                else
                    response = await _httpClient.PutAsJsonAsync($"/api/meetings/{meeting.Id}", meeting);

                if (response.IsSuccessStatusCode)
                    await _localStorage.RemovePendingMeetingAsync(meeting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sync schůzky selhala: {ex.Message}");
            }
        }

        var pendingParticipants = await _localStorage.LoadPendingParticipantsAsync();
        foreach (var kvp in pendingParticipants.ToList())
        {
            foreach (var participant in kvp.Value.ToList())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"/api/meetings/{kvp.Key}/participants", participant);
                    if (response.IsSuccessStatusCode)
                        await _localStorage.RemovePendingParticipantAsync(kvp.Key, participant);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sync účastníka selhala: {ex.Message}");
                }
            }
        }
    }

}

public class RefreshCalendarMessage : ValueChangedMessage<bool>
{
    public RefreshCalendarMessage() : base(true) { }
}
