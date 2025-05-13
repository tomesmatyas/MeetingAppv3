using System.Collections.Generic;

namespace MeetingApp.Models
{
    public class MeetingRecurrence
    {
        public int Id { get; set; }

        // Allowed values: "None", "Weekly", "Monthly"
        public string Pattern { get; set; } = "None";

        public int Interval { get; set; } = 1;

        public List<Meeting> Meetings { get; set; } = new();
    }
}
