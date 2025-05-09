﻿using System.Collections.ObjectModel;

namespace MeetingApp.Models;

public class Meeting
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public DateTime Date { get; set; }
    public bool IsRegular { get; set; } // Pravidelná/nepravidelná schůzka
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ColorHex { get; set; }
    public List<Participant> Participants { get; set; } = new();
}

public class Participant
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int MeetingId { get; set; }
    public Meeting? Meeting { get; set; }
    
}

public class MeetingDisplay
{
    public Meeting Meeting { get; set; } = default!;
    public string Title { get; set; } = "";
    public int GridRow { get; set; }
    public int RowSpan { get; set; }
    public string ColorHex { get; set; } = "#FF6600";
    
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