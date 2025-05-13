
using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;

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

                var settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.None
                };


            }
        }
        catch (Exception ex)
        {

    }

    public async Task<bool> AddMeetingAsync(Meeting meeting)
    {
        try
        {
            if (meeting.IsRegular && meeting.Recurrence != null && meeting.EndDate == null)
            {
                meeting.EndDate = DateTime.Now.AddMonths(6);
            }

            var response = await _httpClient.PostAsJsonAsync("/api/meetings", meeting);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
        }
        catch (Exception ex)
        {

        var offlineMeetings = await _localStorage.LoadMeetingsAsync();
        offlineMeetings.Add(meeting);
        await _localStorage.SaveMeetingsAsync(offlineMeetings);
        Debug.WriteLine("[Offline] Schùzka uložena lokálnì.");
        return false;
    }

    public async Task AddParticipantAsync(int meetingId, MeetingParticipant participant)
    {
        try
        {

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

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi online aktualizaci: {ex.Message}");
        }


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

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pøi detailu: {ex.Message}");
        }

        var offline = await _localStorage.LoadMeetingsAsync();
        return offline.FirstOrDefault(m => m.Id == id);
    }

public class RefreshCalendarMessage : ValueChangedMessage<bool>
{
    public RefreshCalendarMessage() : base(true) { }
}
