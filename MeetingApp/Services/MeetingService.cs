using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging.Messages;

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

    public async Task<List<Meeting>> GetMeetingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/meetings");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(json);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };

                var meetings = JsonSerializer.Deserialize<List<Meeting>>(json, options);

                if (meetings != null)
                {
                    await _localStorage.SaveMeetingsAsync(meetings);
                    Debug.WriteLine("---------------!Schuzky se nacetly a ulozily offline!--------------");
                }

                return meetings ?? new List<Meeting>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Offline rezim aktivovan: {ex.Message}");
        }

        return await _localStorage.LoadMeetingsAsync();
    }

    public async Task<bool> AddMeetingAsync(Meeting meeting)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/meetings", meeting);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pri odesilani schuzky: {ex.Message}");
        }

        // Offline fallback
        var offlineMeetings = await _localStorage.LoadMeetingsAsync();
        offlineMeetings.Add(meeting);
        await _localStorage.SaveMeetingsAsync(offlineMeetings);
        Debug.WriteLine("[Offline] Schùzka uložena lokálnì.");
        return false;
    }

    public async Task AddParticipantAsync(int meetingId, Participant participant)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/meetings/{meetingId}/participants", participant);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pøi pøidávání úèastníka: {ex.Message}");
            await _localStorage.SavePendingParticipantAsync(meetingId, participant);
        }
    }

    public async Task<bool> DeleteMeetingAsync(int meetingId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/meetings/{meetingId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pri mazani: {ex.Message}");
            return false;
        }
    }
    public async Task<bool> UpdateMeetingAsync(Meeting meeting)

    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/meetings/{meeting.Id}", meeting);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi online aktualizaci: {ex.Message}");
        }

        // Offline fallback
        await _localStorage.UpdateMeetingAsync(meeting);
        return true;
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
                Debug.WriteLine($"Sync schùzky selhala: {ex.Message}");
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
                    Debug.WriteLine($"Sync úèastníka selhala: {ex.Message}");
                }
            }
        }
    }
    public async Task<Meeting?> GetMeetingByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/meetings/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Meeting>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve // fix
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pøi detailu: {ex.Message}");
        }

        var offline = await _localStorage.LoadMeetingsAsync();
        return offline.FirstOrDefault(m => m.Id == id);
    }

}

public interface ILocalStorageService
{
    Task SaveMeetingsAsync(List<Meeting> meetings);
    Task<List<Meeting>> LoadMeetingsAsync();
    Task UpdateMeetingAsync(Meeting meeting);
    Task SavePendingParticipantAsync(int meetingId, Participant participant);
    Task<Dictionary<int, List<Participant>>> LoadPendingParticipantsAsync();
    Task<List<Meeting>> LoadPendingMeetingsAsync();
    Task SavePendingMeetingsAsync(List<Meeting> meetings);
    Task ClearPendingMeetingsAsync();
    Task RemovePendingMeetingAsync(Meeting meeting);
    Task RemovePendingParticipantAsync(int meetingId, Participant participant);

}

public class LocalStorageService : ILocalStorageService
{
    private readonly string _filePath;

    public LocalStorageService()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _filePath = Path.Combine(folder, "meetings.json");
    }

    private readonly string _pendingFilePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "pending_participants.json"
);

    public async Task SavePendingParticipantAsync(int meetingId, Participant participant)
    {
        var pending = await LoadPendingParticipantsAsync();

        if (!pending.ContainsKey(meetingId))
            pending[meetingId] = new List<Participant>();

        pending[meetingId].Add(participant);

        var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(_pendingFilePath, json);
    }

    public async Task<Dictionary<int, List<Participant>>> LoadPendingParticipantsAsync()
    {
        try
        {
            if (!File.Exists(_pendingFilePath))
                return new Dictionary<int, List<Participant>>();

            var json = await File.ReadAllTextAsync(_pendingFilePath);
            return JsonSerializer.Deserialize<Dictionary<int, List<Participant>>>(json)
                   ?? new Dictionary<int, List<Participant>>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi ètení pending úèastníkù: {ex.Message}");
            return new Dictionary<int, List<Participant>>();
        }
    }
    public async Task SaveMeetingsAsync(List<Meeting> meetings)
    {
        try
        {
            var json = JsonSerializer.Serialize(meetings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba pøi ukládání do local storage: {ex.Message}");
        }
    }

    public async Task<List<Meeting>> LoadMeetingsAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<Meeting>();

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<Meeting>>(json) ?? new List<Meeting>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba pøi ètení z local storage: {ex.Message}");
            return new List<Meeting>();
        }

    }
    public async Task UpdateMeetingAsync(Meeting updatedMeeting)
    {
        var meetings = await LoadMeetingsAsync();
        var index = meetings.FindIndex(m => m.Id == updatedMeeting.Id);
        if (index >= 0)
        {
            meetings[index] = updatedMeeting;
            await SaveMeetingsAsync(meetings);
        }
    }

    private readonly string _pendingMeetingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "pending_meetings.json"
);

    public async Task<List<Meeting>> LoadPendingMeetingsAsync()
    {
        try
        {
            if (!File.Exists(_pendingMeetingsPath))
                return new List<Meeting>();

            var json = await File.ReadAllTextAsync(_pendingMeetingsPath);
            return JsonSerializer.Deserialize<List<Meeting>>(json) ?? new List<Meeting>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi ètení pending schùzek: {ex.Message}");
            return new List<Meeting>();
        }
    }

    public async Task SavePendingMeetingsAsync(List<Meeting> meetings)
    {
        try
        {
            var json = JsonSerializer.Serialize(meetings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(_pendingMeetingsPath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi ukládání pending schùzek: {ex.Message}");
        }
    }

    public async Task ClearPendingMeetingsAsync()
    {
        try
        {
            if (File.Exists(_pendingMeetingsPath))
                File.Delete(_pendingMeetingsPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi mazání pending schùzek: {ex.Message}");
        }
    }

    public async Task RemovePendingMeetingAsync(Meeting meeting)
    {
        var meetings = await LoadMeetingsAsync();
        meetings.RemoveAll(m => m.Id == meeting.Id || (m.Title == meeting.Title && m.Date == meeting.Date));
        await SaveMeetingsAsync(meetings);
    }

    public async Task RemovePendingParticipantAsync(int meetingId, Participant participant)
    {
        var pending = await LoadPendingParticipantsAsync();

        if (pending.ContainsKey(meetingId))
        {
            var list = pending[meetingId];
            list.RemoveAll(p => p.Name == participant.Name && p.Email == participant.Email);

            if (list.Count == 0)
                pending.Remove(meetingId);

            var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_pendingFilePath, json);
        }
    }


}

public class RefreshCalendarMessage : ValueChangedMessage<bool>
{
    public RefreshCalendarMessage() : base(true) { }
}
