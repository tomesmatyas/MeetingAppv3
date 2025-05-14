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
            RecurrenceId = meeting.RecurrenceId,
            Recurrence = meeting.Recurrence == null ? null : new MeetingRecurrenceDto
            {
                Id = meeting.Recurrence.Id,
                Pattern = meeting.Recurrence.Pattern,
                Interval = meeting.Recurrence.Interval
            },
            CreatedByUserId = meeting.CreatedByUserId,
            CreatedByUser = meeting.CreatedByUser == null ? null : new UserDto
            {
                Id = meeting.CreatedByUser.Id,
                Username = meeting.CreatedByUser.Username,
                Email = meeting.CreatedByUser.Email,
                FirstName = meeting.CreatedByUser.FirstName,
                LastName = meeting.CreatedByUser.LastName
            },
            Participants = meeting.Participants
                .Where(p => p.User != null)
                .Select(p => new MeetingParticipantDto
                {
            MeetingId = p.MeetingId,
            UserId = p.UserId,
            User = new UserDto
            {
                Id = p.User!.Id,
                Username = p.User.Username,
                Email = p.User.Email,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName
            }
    }).ToList()
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
            Recurrence = dto.Recurrence == null ? null : new MeetingRecurrence
            {
                Id = dto.Recurrence.Id,
                Pattern = dto.Recurrence.Pattern,
                Interval = dto.Recurrence.Interval
            },
            CreatedByUserId = dto.CreatedByUserId,
            CreatedByUser = dto.CreatedByUser == null ? null : new User
            {
                Id = dto.CreatedByUser.Id,
                Username = dto.CreatedByUser.Username,
                Email = dto.CreatedByUser.Email,
                FirstName = dto.CreatedByUser.FirstName,
                LastName = dto.CreatedByUser.LastName
            },
            Participants = dto.Participants
            .Where(p => p?.User != null)
            .Select(p => new MeetingParticipant
            {
                UserId = p.UserId,
                User = new User
                {
                    Id = p.User!.Id,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    FirstName = p.User.FirstName,
                    LastName = p.User.LastName
                }
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
