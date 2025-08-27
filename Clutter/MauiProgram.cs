using Clutter.Helpers;
using Clutter.Services;
using Clutter.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.BLE;

namespace Clutter;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiCommunityToolkit()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Rubik-Regular.ttf", "Rubik");
                fonts.AddFont("RubikBeastly-Regular", "RubikBeastly");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<ChatPage>();
        builder.Services.AddSingleton<ChatPageViewModel>();
        builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
        builder.Services.AddSingleton<IMessagingService, MessagingService>();
        builder.Services.AddSingleton<IConnectionService, ConnectionService>();
        builder.Services.AddSingleton(CrossBluetoothLE.Current);
        builder.Services.AddSingleton(CrossBluetoothLE.Current.Adapter);
        builder.Services.AddSingleton<ISoundService, SoundService>();

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}