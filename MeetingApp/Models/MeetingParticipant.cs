namespace MeetingApp.Models
{
    public class MeetingParticipant
    {
        public int MeetingId { get; set; }

        public Meeting? Meeting { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}
