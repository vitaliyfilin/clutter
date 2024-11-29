using Plugin.BLE.Abstractions.Contracts;

namespace Clutter.Services;

public class MessagingService : IMessagingService
{
    public Task<List<(IDevice Device, bool Success, string? ErrorMessage)>> SendToDevicesAsync(HashSet<IDevice> devices, string message)
    {
        throw new NotImplementedException();
    }
}