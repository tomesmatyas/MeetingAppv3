using MeetingApp.Pages;

namespace MeetingApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AddMeetingPage), typeof(AddMeetingPage));
        }
    }
}
