using Android.Bluetooth.LE;

namespace Clutter.Services;

public sealed class MyAdvertiseCallback : AdvertiseCallback
{
    public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
    {
        base.OnStartSuccess(settingsInEffect);
        Console.WriteLine("Advertisement started successfully");
    }

    public override void OnStartFailure(AdvertiseFailure errorCode)
    {
        base.OnStartFailure(errorCode);
        Console.WriteLine($"Advertisement failed: {errorCode}");
    }
}