using Hibabejelento.Maui.Services;
using Hibabejelento.Maui.ViewModels;
using Hibabejelento.Maui.Views;
using Microsoft.Extensions.Logging;

namespace Hibabejelento.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // HTTP + ApiService
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IApiService, ApiService>();

        // ViewModel
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<TicketCreationViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<FilterViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<SearchViewModel>();
        builder.Services.AddTransient<TicketDetailsViewModel>();
        builder.Services.AddTransient<TicketEditViewModel>();        

        // View
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<TicketCreationPage>();
        builder.Services.AddTransient<TicketDetailsPage>();
        builder.Services.AddTransient<TicketEditPage>();
        builder.Services.AddTransient<SearchPage>();
        builder.Services.AddTransient<FilterPage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}