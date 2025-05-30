﻿using System;
using System.Collections.Generic;

namespace MeetingApp.Models.Dtos;
public class UpdateMeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ColorHex { get; set; }
    public bool IsRegular { get; set; }
    public int? RecurrenceId { get; set; }
    public int? Interval { get; set; }
    public DateTime? EndDate { get; set; }
    public int CreatedByUserId { get; set; }

    // ✅ správně pro API
    public List<int> Participants { get; set; } = new();
}
public class CreateMeetingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ColorHex { get; set; }
    public bool IsRegular { get; set; }
    public int? RecurrenceId { get; set; }
    public int? Interval { get; set; }
    public DateTime? EndDate { get; set; }
    public int CreatedByUserId { get; set; }
    public List<int> Participants { get; set; } = new();
}


public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
    public string FullName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? Username
            : $"{FirstName} {LastName}".Trim();
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class UpdateUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Username { get; set; } = string.Empty;
}



public class MeetingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime? EndDate { get; set; }
    public string ColorHex { get; set; } = "#0078D7";
    public bool IsRegular { get; set; }
    public int? RecurrenceId { get; set; }
    public string? RecurrencePattern { get; set; }
    public int Interval { get; set; } = 1;
    public int CreatedByUserId { get; set; }

    // ⛳ API expects participant IDs only
    public List<MeetingParticipantDto> Participants { get; set; } = new();
    public List<UserDto> ParticipantUsers { get; set; } = new();
  
}



public class MeetingRecurrenceDto
{
    public int Id { get; set; }
    public string Pattern { get; set; } = "None";
    public int Interval { get; set; } = 1;
}

public class MeetingParticipantDto
{
    public int MeetingId { get; set; }
    public int UserId { get; set; }
    public UserDto? User { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

