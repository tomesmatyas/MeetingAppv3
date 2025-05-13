using System.Collections.Generic;

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

        public string FullName => $"{FirstName} {LastName}".Trim();

        public List<MeetingParticipant> Meetings { get; set; } = new();

        public List<Meeting> CreatedMeetings { get; set; } = new();
    }
}
