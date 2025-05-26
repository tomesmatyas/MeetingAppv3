// File: Services/Mapping/MeetingMapper.cs
using MeetingApp.Models;
using MeetingApp.Models.Dtos;

namespace MeetingApp.Services;

public static class MeetingMapper
{
    public static MeetingDto MapToDto(Meeting meeting)
    {
        return new MeetingDto
        {
            Id = meeting.Id,
            Title = meeting.Title,
            Date = meeting.Date,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            EndDate = meeting.EndDate,
            ColorHex = meeting.ColorHex,
            IsRegular = meeting.IsRegular,
            RecurrenceId = meeting.RecurrenceId ?? 0,
            RecurrencePattern = meeting.Recurrence?.Pattern,
            Interval = meeting.Recurrence?.Interval ?? 1,
            CreatedByUserId = meeting.CreatedByUserId,

            Participants = meeting.Participants
                .Where(p => p.UserId != 0)
                .Select(p => new MeetingParticipantDto
                {
                    MeetingId = p.MeetingId,
                    UserId = p.UserId,
                    User = p.User != null ? new UserDto
                    {
                        Id = p.User.Id,
                        Username = p.User.Username,
                        Email = p.User.Email,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        Role = p.User.Role
                    } : null
                }).ToList()
        };
    }

    public static UpdateMeetingDto ToUpdateDto(this MeetingDto meeting)
    {
        return new UpdateMeetingDto
        {
            Id = meeting.Id,
            Title = meeting.Title,
            Date = meeting.Date,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            ColorHex = meeting.ColorHex,
            IsRegular = meeting.IsRegular,
            RecurrenceId = meeting.RecurrenceId,
            Interval = meeting.Interval,
            EndDate = meeting.EndDate,
            CreatedByUserId = meeting.CreatedByUserId,
            Participants = meeting.Participants
                .Where(p => p != null)
                .Select(p => p.UserId)
                .ToList()
        };
    }


    public static Meeting MapToModel(MeetingDto dto)
    {
        return new Meeting
        {
            Id = dto.Id,
            Title = dto.Title,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            EndDate = dto.EndDate,
            ColorHex = dto.ColorHex,
            IsRegular = dto.IsRegular,
            RecurrenceId = dto.RecurrenceId,
            Recurrence = !string.IsNullOrEmpty(dto.RecurrencePattern) ? new MeetingRecurrence
            {
                Id = dto.RecurrenceId ?? 0,
                Pattern = dto.RecurrencePattern,
                Interval = dto.Interval
            } : null,
            CreatedByUserId = dto.CreatedByUserId,

            Participants = dto.Participants
                    .Select(p => new MeetingParticipant
                    {
                        UserId = p.UserId,
                        User = p.User != null ? new User
                        {
                            Id = p.User.Id,
                            Username = p.User.Username,
                            Email = p.User.Email,
                            FirstName = p.User.FirstName,
                            LastName = p.User.LastName,
                            Role = p.User.Role
                        } : null
                    }).ToList()

        };
    }

    public static MeetingDto? SanitizeDto(MeetingDto? meeting)
    {
        if (meeting == null) return null;

        meeting.Participants = meeting.Participants
            ?.Where(p => p != null)
            .ToList() ?? new();

        return meeting;
    }
}
