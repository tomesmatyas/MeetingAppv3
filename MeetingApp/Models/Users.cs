using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeetingApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string FullName =>
    string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Username
        : $"{FirstName} {LastName}".Trim();

        [JsonIgnore]
        public List<MeetingParticipant> Meetings { get; set; } = new();

        [JsonIgnore]
        public List<Meeting> CreatedMeetings { get; set; } = new();
    }
}