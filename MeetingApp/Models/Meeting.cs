using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MeetingApp.Models;

public class Meeting
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ColorHex { get; set; }
    public bool IsRegular { get; set; }

    public int? RecurrenceId { get; set; }
    public MeetingRecurrence? Recurrence { get; set; }

    public List<MeetingParticipant> Participants { get; set; } = new();
}

public class MeetingRecurrence
{
    public int Id { get; set; }
    public string Pattern { get; set; } = "None"; // 'None', 'Weekly', 'Monthly'
    public int Interval { get; set; } = 1;
    public DateTime? EndDate { get; set; }

    public List<Meeting> Meetings { get; set; } = new();
}

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public List<MeetingParticipant> Meetings { get; set; } = new();
}

public class MeetingParticipant
{
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class MeetingDisplay
{
    public Meeting Meeting { get; set; } = default!;
    public string Title { get; set; } = "";
    public int GridRow { get; set; }
    public int RowSpan { get; set; }
    public string ColorHex { get; set; } = "#FF6600";
    public int Id => Meeting.Id;
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
