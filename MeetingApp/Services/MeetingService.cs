using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MeetingApp.Services;
public class MeetingService
{
    private readonly HttpClient _httpClient;

    public MeetingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:5091"); // Bez /swagger!
    }

    public async Task<List<Meeting>?> GetMeetingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/meetings");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(json); // log kontroln� v�stup

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                Debug.WriteLine($"---------------!Schuzky se na�etly!--------------");
                return JsonSerializer.Deserialize<List<Meeting>>(json, options);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba p�i na��t�n� sch�zek: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> AddMeetingAsync(Meeting meeting)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/meetings", meeting);
            return response.IsSuccessStatusCode;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Chyba p�i odes�l�n�: {ex.Message}");
            return false;
        }
    }

    public async Task AddParticipantAsync(int meetingId, Participant participant)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/meetings/{meetingId}/participants", participant);
        response.EnsureSuccessStatusCode();
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
            Debug.WriteLine($"Chyba p�i maz�n�: {ex.Message}");
            return false;
        }
    }
}