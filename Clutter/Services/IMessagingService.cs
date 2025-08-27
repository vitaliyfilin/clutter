using Plugin.BLE.Abstractions.Contracts;

namespace Clutter.Services;

public interface IMessagingService
{
    Task<List<(IDevice Device, 
        bool Success, 
        string? ErrorMessage)>> SendToDevicesAsync(HashSet<IDevice> devices, string message);
}