
using MeetingApp.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using Newtonsoft.Json;
using MeetingApp.Services;
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

                var meetings = JsonConvert.DeserializeObject<List<Meeting>>(json, settings);

                if (meetings != null)
                {
                    foreach (var meeting in meetings)
                    {
                        meeting.IsRegular = meeting.Recurrence != null;
                        meeting.RecurrenceId = meeting.Recurrence?.Id;
                    }

                    var expanded = ExpandRecurringMeetings(meetings);
                    await _localStorage.SaveMeetingsAsync(expanded);
                    return expanded;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Offline režim aktivován: {ex.Message}");
        }

        return await _localStorage.LoadMeetingsAsync();
    }

    private List<Meeting> ExpandRecurringMeetings(List<Meeting> meetings)
    {
        var result = new List<Meeting>();
        var rangeStart = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
        var rangeEnd = rangeStart.AddDays(6);

        foreach (var m in meetings)
        {
            if (!m.IsRegular || m.Recurrence == null)
            {
                result.Add(m);
                continue;
            }

            var rec = m.Recurrence;
            var endDate = m.EndDate ?? rangeEnd;
            var current = m.Date;
            var instanceDate = rangeStart;

            while (instanceDate <= rangeEnd && instanceDate <= endDate)
            {
                if (rec.Pattern == "Weekly" && (instanceDate - current).Days % (7 * rec.Interval) == 0)
                {
                    result.Add(CloneMeeting(m, instanceDate));
                }
                else if (rec.Pattern == "Monthly" && current.Day == instanceDate.Day)
                {
                    var monthsBetween = ((instanceDate.Year - current.Year) * 12) + instanceDate.Month - current.Month;
                    if (monthsBetween % rec.Interval == 0)
                    {
                        result.Add(CloneMeeting(m, instanceDate));
                    }
                }

                instanceDate = instanceDate.AddDays(1);
            }
        }

        return result;
    }

    private Meeting CloneMeeting(Meeting source, DateTime date)
    {
        return new Meeting
        {
            Id = source.Id,
            Title = source.Title,
            Date = date,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            ColorHex = source.ColorHex,
            IsRegular = true,
            RecurrenceId = source.RecurrenceId,
            Recurrence = source.Recurrence,
            Participants = source.Participants
        };
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
            Debug.WriteLine($"[Offline] Chyba pøi odesílání schùzky: {ex.Message}");
        }

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
            var payload = new { userId = participant.UserId };
            await _httpClient.PostAsJsonAsync($"/api/meetings/{meetingId}/participants", payload);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pøi pøidávání úèastníka: {ex.Message}");
            await _localStorage.SavePendingParticipantAsync(meetingId, participant);
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
            Debug.WriteLine($"Chyba pøi odebírání úèastníka: {ex.Message}");
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
            Debug.WriteLine($"Chyba pøi mazání: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateMeetingAsync(Meeting meeting)
    {
        try
        {
            if (meeting.IsRegular && meeting.Recurrence != null && meeting.EndDate == null)
            {
                meeting.EndDate = DateTime.Now.AddMonths(6);
            }

            var response = await _httpClient.PutAsJsonAsync($"/api/meetings/{meeting.Id}", meeting);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Chyba pøi online aktualizaci: {ex.Message}");
        }

        await _localStorage.UpdateMeetingAsync(meeting);
        return true;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/users");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<List<User>>(json);

                if (users != null)
                {
                    var nonAdmins = users.Where(u => !u.IsAdmin).ToList();
                    await _localStorage.SaveUsersAsync(nonAdmins);
                    return nonAdmins;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Offline] Chyba pøi naèítání uživatelù: {ex.Message}");
        }

        var offlineUsers = await _localStorage.LoadUsersAsync();
        return offlineUsers.Where(u => !u.IsAdmin).ToList();
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
                return JsonConvert.DeserializeObject<Meeting>(json);
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

public class RefreshCalendarMessage : ValueChangedMessage<bool>
{
    public RefreshCalendarMessage() : base(true) { }
}
