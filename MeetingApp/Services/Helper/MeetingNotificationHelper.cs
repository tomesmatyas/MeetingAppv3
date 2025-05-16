using MeetingApp.Models.Dtos;
using Plugin.LocalNotification;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using Plugin.LocalNotification.iOSOption;
namespace MeetingApp.Services.Helper;
public static class MeetingNotificationHelper
{
    public static async Task ScheduleNotificationAsync(MeetingDto meeting)
    {
        var notifyTime = meeting.Date.Add(meeting.StartTime).AddMinutes(-15);

        if (notifyTime > DateTime.Now)
        {
            var notification = new NotificationRequest
            {
                NotificationId = meeting.Id, // unikátní ID = ID schůzky
                Title = "Připomínka schůzky",
                Description = $"Za 15 minut začíná: {meeting.Title}",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
    }

    public static void CancelNotification(int meetingId)
    {
        LocalNotificationCenter.Current.Cancel(meetingId);
    }
}
