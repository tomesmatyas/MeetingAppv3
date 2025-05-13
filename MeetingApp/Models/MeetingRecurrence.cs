using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeetingApp.Models
{
    public class MeetingRecurrence
    {
        public int Id { get; set; }

        // Allowed values: "None", "Weekly", "Monthly"
        public string Pattern { get; set; } = "None";

        public int Interval { get; set; } = 1;

        [JsonIgnore]
        public List<Meeting> Meetings { get; set; } = new();
    }
}
