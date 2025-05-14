using MeetingApp.Services;
using MeetingApp.Services.Auth;

namespace MeetingApp
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MeetingService _meetingService;

        public App(IServiceProvider serviceProvider, MeetingService meetingService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _meetingService = meetingService;

            SetMainPageBasedOnLogin();

            // Automatická synchronizace offline dat
            Task.Run(async () =>
            {
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                    await _meetingService.SyncPendingChangesAsync();
            });
        }

        private async void SetMainPageBasedOnLogin()
        {
            MainPage = _serviceProvider.GetRequiredService<AppShell>();

            await Task.Delay(200); // počkej na inicializaci Shell.Current

            var token = Preferences.Get("access_token", string.Empty);
            if (!string.IsNullOrEmpty(token))
                await Shell.Current.GoToAsync("//CalendarPage");
            else
                await Shell.Current.GoToAsync("//LoginPage");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_serviceProvider.GetRequiredService<AppShell>());
        }
    }
}