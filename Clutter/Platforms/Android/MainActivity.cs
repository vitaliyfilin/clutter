using Android;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Handlers;
using Color = Android.Graphics.Color;

namespace Clutter;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private readonly string[] _permissions =
    {
        Manifest.Permission.Bluetooth, 
        Manifest.Permission.BluetoothAdmin, 
        Manifest.Permission.BluetoothPrivileged,

        // Android 12+ (API level 31+)
#pragma warning disable CA1416
        Manifest.Permission.BluetoothScan, 
        Manifest.Permission.BluetoothConnect, 
        Manifest.Permission.BluetoothAdvertise, 
#pragma warning restore CA1416

        // Pre-Android 12 (API level < 31)
        Manifest.Permission.AccessCoarseLocation, 
        Manifest.Permission.AccessFineLocation,
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        CheckPermissions();
        RequestedOrientation = ScreenOrientation.Portrait;
        base.OnCreate(savedInstanceState);

        // Make the navigation bar transparent
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            Window?.SetNavigationBarColor(Color.ParseColor("#000000"));
        }

        // Remove Entry control underline
        EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
        {
            h.PlatformView.BackgroundTintList =
                ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
        });
    }

    private void CheckPermissions()
    {
        var minimumPermissionsGranted = true;

        foreach (var permission in _permissions)
        {
            switch (Build.VERSION.SdkInt)
            {
#pragma warning disable CA1416
                case >= BuildVersionCodes.S when
                    permission is Manifest.Permission.AccessCoarseLocation or Manifest.Permission.AccessFineLocation:
                    continue;
                case >= BuildVersionCodes.S:
                {
                    // Check for the BluetoothConnect permission specifically for Android 12+
                    if (permission == Manifest.Permission.BluetoothConnect &&
                        CheckSelfPermission(permission) != Permission.Granted)
                    {
                        minimumPermissionsGranted = false;
                    }

                    break;
                }
                default:
                {
                    // For devices lower than Android 12, check other permissions as needed
                    if (CheckSelfPermission(permission) != Permission.Granted)
                    {
                        minimumPermissionsGranted = false;
                    }

                    break;
                }
            }
        }

        if (!minimumPermissionsGranted)
        {
            RequestPermissions(_permissions, 0);
#pragma warning restore CA1416
        }
    }
}