using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace Clutter.Helpers;

public static class ToastHelper
{
    public static async Task ShowExceptionToast(Exception exception)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Toast.Make(exception.Message, ToastDuration.Long, textSize: 18D).Show();
        });
    }

    public static async Task ShowInfoToast(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Toast.Make(message, ToastDuration.Long, textSize: 18D).Show();
        });
    }
}