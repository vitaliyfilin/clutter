using Plugin.BLE.Abstractions.Contracts;

namespace Clutter.Services;

public class ConnectionService : IConnectionService
{
    public Task ConnectToDeviceAsync(IDevice device)
    {
        throw new NotImplementedException();
    }

    public ICharacteristic? ChatCharacteristic { get; set; }
}