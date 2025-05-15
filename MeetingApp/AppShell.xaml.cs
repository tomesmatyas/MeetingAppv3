using MeetingApp.Models.ViewModels;
using MeetingApp.Pages;
using MeetingApp.Services.Auth;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
namespace MeetingApp;
public partial class AppShell : Shell
{
    private readonly AuthGuardService _authGuard;
    private readonly UserSession _userSession;

    public AppShell(AppShellViewModel vm,AuthGuardService authGuard, UserSession userSession)
    {
        InitializeComponent();
        _authGuard = authGuard;

        Routing.RegisterRoute(nameof(AddMeetingPage), typeof(AddMeetingPage));
        Routing.RegisterRoute(nameof(MeetingDetailPage), typeof(MeetingDetailPage));

        Navigating += AppShell_Navigating;
        _userSession = userSession;
        BindingContext = vm;
        Debug.WriteLine("[AppShell] BindingContext nastaven na AppShellViewModel");
        Navigated += AppShell_Navigated;
    }
    private void AppShell_Navigated(object? sender, ShellNavigatedEventArgs e)
    {
        var currentRoute = Shell.Current.CurrentState.Location.OriginalString;

        Shell.Current.FlyoutBehavior = currentRoute.Contains("LoginPage")
            ? FlyoutBehavior.Disabled
            : FlyoutBehavior.Flyout;
    }
    private async void AppShell_Navigating(object? sender, ShellNavigatingEventArgs e)
    {
        // Povol přechod na login nebo root
        if (e.Target.Location.OriginalString.Contains("LoginPage") || e.Target.Location.OriginalString == "//")
            return;

        var isValid = await _authGuard.EnsureAuthenticatedAsync();
        if (!isValid)
        {
            e.Cancel(); // zastaví navigaci
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
