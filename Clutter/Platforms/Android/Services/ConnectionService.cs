using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using static Plugin.BLE.CrossBluetoothLE;

namespace Clutter.Services;

public class ConnectionService : IConnectionService
{
    private readonly IAdapter _adapter;
    public ICharacteristic? ChatCharacteristic { get; set; }

    public ConnectionService()
    {
        _adapter = Current.Adapter;
    }

    public async Task ConnectToDeviceAsync(IDevice device)
    {
        if (!CheckDevice(device)) return;
        try
        {
            await _adapter.ConnectToDeviceAsync(device);

            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                foreach (var characteristic in characteristics)
                {
                    if (!characteristic.CanWrite || !characteristic.CanUpdate) continue;

                    ChatCharacteristic = characteristic;

                    await ChatCharacteristic.StartUpdatesAsync();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}, {e.Message}");
        }
    }

    private bool CheckDevice(IDevice device)
    {
        if (device.Name is null) return false;
        if (string.Empty.Equals(device.Name)) return false;
        if (!device.IsConnectable || !device.SupportsIsConnectable) return false;
        if (device.State is DeviceState.Connected or DeviceState.Connecting) return false;
        if (_adapter.ConnectedDevices.Any(d => d.Id == device.Id)) return false;
        return !string.IsNullOrWhiteSpace(device.Name);
    }
}