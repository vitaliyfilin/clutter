namespace Clutter.Services;

public interface ISoundService
{
    Task PlayDiscoveredSoundAsync();
    Task PlayReceivedMessageSoundAsync();
}