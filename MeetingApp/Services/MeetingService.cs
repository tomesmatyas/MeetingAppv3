
using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using Newtonsoft.Json;
using MeetingApp.Services;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MeetingApp.Models.Dtos;
using System.Net.Http.Headers;

namespace MeetingApp.Services;

public class MeetingService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public MeetingService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;

        

        Connectivity.ConnectivityChanged += async (s, e) =>
        {
            if (e.NetworkAccess.HasFlag(NetworkAccess.Internet))
                await SyncPendingChangesAsync();
        };


        Connectivity.ConnectivityChanged += async (s, e) =>
        {
            if (e.NetworkAccess.HasFlag(NetworkAccess.Internet))
                await SyncPendingChangesAsync();
        };
    }
    public async Task InitAsync()
    {
        var token = await _localStorage.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Debug.WriteLine("TOKEN >>> " + token); // ✅ loguj až po nastavení
        }
        else
        {
            Debug.WriteLine("❌ Token není dostupný");
        }
    }


    public async Task<List<MeetingDto>> GetMeetingsAsync()
    {
        try
        {
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Debug.WriteLine($" Header: {header.Key} = {string.Join(", ", header.Value)}");
            }
            var response = await _httpClient.GetAsync("/api/meetings");
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[❌ API Error] {response.StatusCode}: {content}");
            }
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var meetings = JsonConvert.DeserializeObject<List<MeetingDto>>(json);

                foreach (var m in meetings)
                    Debug.WriteLine("Title = " + m.Title);

                return meetings?
                    .Select(MeetingMapper.SanitizeDto)
                    .Where(m => m != null)
                    .ToList() ?? new();
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
                var dto = JsonConvert.DeserializeObject<MeetingDto>(json);
                Debug.WriteLine(json);
                return MeetingMapper.SanitizeDto(dto);
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
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba AddMeeting: {ex.Message}");
        }

        // Offline fallback
        var model = MeetingMapper.MapToModel(meeting);
        await _localStorage.SavePendingMeetingsAsync(new List<Meeting> { model });
        return false;
    }


    public async Task<bool> UpdateMeetingAsync(MeetingDto meeting)
    {
        try
        {
            if (meeting.IsRegular && meeting.Recurrence != null && meeting.EndDate == null)
                meeting.EndDate = DateTime.Now.AddMonths(6);

            var response = await _httpClient.PutAsJsonAsync($"/api/meetings/{meeting.Id}", meeting);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[❌ API Error] {response.StatusCode}: {content}");
            }
            else {
                return true; 
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] UpdateMeetingAsync selhal: {ex.Message}");
        }

        // Offline fallback
        var model = MeetingMapper.MapToModel(meeting);
        var pending = await _localStorage.LoadPendingMeetingsAsync();
        pending.RemoveAll(m => m.Id == model.Id);
        pending.Add(model);
        await _localStorage.SavePendingMeetingsAsync(pending);
        return false;
    }
    public async Task<bool> CreateMeetingAsync(MeetingDto dto)
    {
        try
        {
            var token = await _localStorage.GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Přidej userId vytvořitele
            if (dto.CreatedByUserId == 0)
            {
                var idStr = await SecureStorage.Default.GetAsync("user_id");
                if (!int.TryParse(idStr, out int userId))
                    return false;

                dto.CreatedByUserId = userId;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/meetings", dto);

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine("[API] Schůzka vytvořena.");
                return true;
            }

            Debug.WriteLine($"[API] Vytvoření selhalo: {(int)response.StatusCode} {response.ReasonPhrase}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API] Chyba při vytváření schůzky: {ex.Message}");
            return false;
        }
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
            var payload = new { userId };
            await _httpClient.PostAsJsonAsync($"/api/meetings/{meetingId}/participants", payload);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] AddParticipantAsync selhal: {ex.Message}");

            // Offline fallback
            var user = (await _localStorage.LoadUsersAsync()).FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                var participant = new MeetingParticipant
                {
                    MeetingId = meetingId,
                    UserId = userId,
                    User = user
                };

                await _localStorage.SavePendingParticipantAsync(meetingId, participant);
            }
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
