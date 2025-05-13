using MeetingApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeetingApp.Services
{
    public interface ILocalStorageService
    {
        Task SaveMeetingsAsync(List<Meeting> meetings);
        Task<List<Meeting>> LoadMeetingsAsync();
        Task UpdateMeetingAsync(Meeting meeting);
        Task SavePendingParticipantAsync(int meetingId, MeetingParticipant participant);
        Task<Dictionary<int, List<MeetingParticipant>>> LoadPendingParticipantsAsync();
        Task<List<Meeting>> LoadPendingMeetingsAsync();
        Task SavePendingMeetingsAsync(List<Meeting> meetings);
        Task ClearPendingMeetingsAsync();
        Task RemovePendingMeetingAsync(Meeting meeting);
        Task RemovePendingParticipantAsync(int meetingId, MeetingParticipant participant);

        Task SaveUsersAsync(List<User> users);
        Task<List<User>> LoadUsersAsync();
    }

    public class LocalStorageService : ILocalStorageService
    {
        private readonly string _filePath;
        private readonly string _usersFilePath;

        public LocalStorageService()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(folder, "meetings.json");
            _usersFilePath = Path.Combine(folder, "users.json");
        }

        private readonly string _pendingFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "pending_participants.json"
        );

        public async Task SaveUsersAsync(List<User> users)
        {
            try
            {
                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await File.WriteAllTextAsync(_usersFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při ukládání uživatelů: {ex.Message}");
            }
        }

        public async Task<List<User>> LoadUsersAsync()
        {
            try
            {
                if (!File.Exists(_usersFilePath))
                    return new List<User>();

                var json = await File.ReadAllTextAsync(_usersFilePath);
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při čtení uživatelů: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task SavePendingParticipantAsync(int meetingId, MeetingParticipant participant)
        {
            var pending = await LoadPendingParticipantsAsync();

            if (!pending.ContainsKey(meetingId))
                pending[meetingId] = new List<MeetingParticipant>();

            pending[meetingId].Add(participant);

            var json = JsonSerializer.Serialize(pending, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_pendingFilePath, json);
        }

        public async Task<Dictionary<int, List<MeetingParticipant>>> LoadPendingParticipantsAsync()
        {
            try
            {
                if (!File.Exists(_pendingFilePath))
                    return new Dictionary<int, List<MeetingParticipant>>();

                var json = await File.ReadAllTextAsync(_pendingFilePath);
                return JsonSerializer.Deserialize<Dictionary<int, List<MeetingParticipant>>>(json)
                       ?? new Dictionary<int, List<MeetingParticipant>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba při čtení pending účastníků: {ex.Message}");
                return new Dictionary<int, List<MeetingParticipant>>();
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
                Console.WriteLine($"Chyba při ukládání do local storage: {ex.Message}");
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
                Console.WriteLine($"Chyba při čtení z local storage: {ex.Message}");
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
                Debug.WriteLine($"Chyba při čtení pending schůzek: {ex.Message}");
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
                Debug.WriteLine($"Chyba při ukládání pending schůzek: {ex.Message}");
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
                Debug.WriteLine($"Chyba při mazání pending schůzek: {ex.Message}");
            }
        }

        public async Task RemovePendingMeetingAsync(Meeting meeting)
        {
            var meetings = await LoadMeetingsAsync();
            meetings.RemoveAll(m => m.Id == meeting.Id || (m.Title == meeting.Title && m.Date == meeting.Date));
            await SaveMeetingsAsync(meetings);
        }

        public async Task RemovePendingParticipantAsync(int meetingId, MeetingParticipant participant)
        {
            var pending = await LoadPendingParticipantsAsync();

            if (pending.ContainsKey(meetingId))
            {
                var list = pending[meetingId];
                list.RemoveAll(p => p.UserId == participant.UserId);

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
}