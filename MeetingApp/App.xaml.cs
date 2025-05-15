using MeetingApp.Pages;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using System.Diagnostics;

namespace MeetingApp
{
    public partial class App : Application
    {
        //private readonly IServiceProvider _serviceProvider;
        //private readonly MeetingService _meetingService;
        private readonly AppShell _shell;
        private readonly UserSession _userSession;
        public App(AppShell shell, UserSession userSession)
        {
            InitializeComponent();
            _shell = shell;
            _userSession = userSession;

            // Volání přes instanci
            Task.Run(async () => await _userSession.LoadAsync());

            //_serviceProvider = serviceProvider;
            //_meetingService = meetingService;

            // SetMainPageBasedOnLogin();

            // Automatická synchronizace offline dat
            //Task.Run(async () =>
            //{
            //    if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            //        await _meetingService.SyncPendingChangesAsync();
            //});
        }

        //private async void SetMainPageBasedOnLogin()
        //{
        //    try
        //    {
        //        MainPage = _serviceProvider.GetRequiredService<AppShell>();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("[FATAL] AppShell failed: " + ex.Message);
        //    }

        //    await Task.Delay(200); // počkej na inicializaci Shell.Current

        //    var token = Preferences.Get("access_token", string.Empty);
        //    if (!string.IsNullOrEmpty(token))
        //        await Shell.Current.GoToAsync("//CalendarPage");
        //    else
        //        await Shell.Current.GoToAsync("//LoginPage");
        //}

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_shell);
        }
    }
}