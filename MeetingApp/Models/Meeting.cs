using MeetingApp.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MeetingApp.Models
{ 
    public class Meeting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ColorHex { get; set; }
        public bool IsRegular { get; set; }
        public int? RecurrenceId { get; set; }
        public MeetingRecurrence? Recurrence { get; set; }
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public List<MeetingParticipant> Participants { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class MeetingDisplay
    {
        public MeetingDto Meeting { get; set; } = default!;
        public int GridRow { get; set; }
        public int RowSpan { get; set; }
        public string ColorHex => Meeting.ColorHex ?? "#FF6600";
        public int Id => Meeting.Id;
        public string Title => Meeting.Title;
        public TimeSpan StartTime => Meeting.StartTime;
        public TimeSpan EndTime => Meeting.EndTime;
        public string TimeRange => $"{Meeting.StartTime:hh\\:mm}–{Meeting.EndTime:hh\\:mm}";
        public string ParticipantInfo => Meeting.Participants?.Count.ToString() ?? "0";
    }

    public class DayModel   
    {   
        public int ColumnIndex { get; set; }
        public DateTime Date { get; set; }
        public ObservableCollection<MeetingDisplay> Meetings { get; set; } = new();
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
