using System.Diagnostics;
using Android.Content;
using Android.Media;
using Application = Android.App.Application;

namespace Clutter.Services;

public sealed class SoundService : ISoundService
{
    public async Task PlayDiscoveredSoundAsync()
    {
        await PlaySoundFromAssetAsync("discovered.mp3");
    }

    public async Task PlayReceivedMessageSoundAsync()
    {
        await PlaySoundFromAssetAsync("received.wav");
    }

    private static async Task PlaySoundFromAssetAsync(string fileName)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            await Task.Delay(500);

            var tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await using (var tempFileStream = File.Create(tempFilePath))
            {
                await stream.CopyToAsync(tempFileStream);
            }

            var mediaPlayer = new MediaPlayer();
            await mediaPlayer.SetDataSourceAsync(tempFilePath);

            mediaPlayer.Prepare();

             mediaPlayer.Start();
             
            mediaPlayer.Completion += (_, _) =>
            {
                mediaPlayer.Release();
                mediaPlayer.Dispose();
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error playing sound: {ex.Message}");
        }
    }
}