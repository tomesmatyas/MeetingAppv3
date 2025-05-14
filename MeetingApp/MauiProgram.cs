// File: MauiProgram.cs
using CommunityToolkit.Maui;
using MeetingApp.Models.ViewModels;
using MeetingApp.Pages;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using MeetingApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MeetingApp
{
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

            // Services
            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost:5091") });
            builder.Services.AddSingleton<MeetingService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();


            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<CalendarViewModel>();
            builder.Services.AddTransient<AppShellViewModel>();
            builder.Services.AddTransient<MeetingDetailViewModel>();

            // Pages
            builder.Services.AddTransient<AppShell>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<CalendarPage>();
            builder.Services.AddTransient<MeetingDetailPage>();

            return builder.Build();
        }
    }
}
