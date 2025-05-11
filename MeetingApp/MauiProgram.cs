using CommunityToolkit.Maui;
using MeetingApp.Models.ViewModels;
using MeetingApp.Pages;
using MeetingApp.Services;
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

            builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost:5091") });
            builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
            // Registrace MeetingService
            builder.Services.AddSingleton<MeetingService>();

            // Registrace ViewModelu a stránky
            builder.Services.AddTransient<TestViewModel>();
            builder.Services.AddTransient<CalendarViewModel>();
            builder.Services.AddTransient<AddMeetingViewModel>();

              builder.Services.AddTransient<TestPage>();
            builder.Services.AddTransient<AddMeetingPage>();
            builder.Services.AddTransient<CalendarPage>();

            builder.Services.AddTransient<MeetingDetailViewModel>();
            builder.Services.AddTransient<MeetingDetailPage>();
            
#endif

            return builder.Build();
        }
    }
}
