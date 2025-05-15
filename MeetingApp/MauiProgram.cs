// File: MauiProgram.cs
using CommunityToolkit.Maui;
using MeetingApp.Models.ViewModels;
using MeetingApp.Pages;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using MeetingApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MeetingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ⬇️ Před DI registrací: načteme session synchronně
        var session = UserSession.Instance;
        Task.Run(() => session.LoadAsync()).Wait(); // synchronní fallback

        // Services
        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://192.168.0.178:5000")
            };
            return client;
        });

        builder.Services.AddSingleton<MeetingService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddSingleton<AuthGuardService>();
        builder.Services.AddSingleton(session); // session je už načtená

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<AppShellViewModel>();
        builder.Services.AddTransient<MeetingDetailViewModel>();
        builder.Services.AddTransient<AddMeetingViewModel>();

        // Pages
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<MeetingDetailPage>();
        builder.Services.AddTransient<AddMeetingPage>();

        return builder.Build();
    }
}
