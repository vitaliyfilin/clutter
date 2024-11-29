using Plugin.BLE.Abstractions.Contracts;

namespace Clutter.Services;

public interface IConnectionService
{
    Task ConnectToDeviceAsync(IDevice device);
    ICharacteristic? ChatCharacteristic { get; set; }
}