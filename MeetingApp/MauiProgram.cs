﻿// File: MauiProgram.cs
using CommunityToolkit.Maui;
using MeetingApp.Models.ViewModels;
using MeetingApp.Pages;
using MeetingApp.Services;
using MeetingApp.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace MeetingApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
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
                BaseAddress = new Uri("http://localhost:5091")//192.168.0.178 localhost:5091
            };
            return client;
        });

        builder.Services.AddSingleton<MeetingService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddSingleton<AuthGuardService>();
        builder.Services.AddSingleton(session);
        

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<AppShellViewModel>();
        builder.Services.AddTransient<MeetingDetailViewModel>();
        builder.Services.AddTransient<AddMeetingViewModel>();
        builder.Services.AddTransient<RegistrationViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();


        // Pages
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<MeetingDetailPage>();
        builder.Services.AddTransient<AddMeetingPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }
}
