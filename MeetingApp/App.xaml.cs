using MeetingApp.Services;

namespace MeetingApp
{
    public partial class App : Application
    {
        private readonly MeetingService _meetingService;

        public App(MeetingService meetingService)
        {
            InitializeComponent();
            _meetingService = meetingService;

            // Automatická synchronizace offline dat
            Task.Run(async () =>
            {
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    await _meetingService.SyncPendingChangesAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
