using Clutter.Helpers;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using static Plugin.BLE.CrossBluetoothLE;

namespace Clutter.Services;

public class ConnectionService : IConnectionService
{
    private readonly IAdapter _adapter = Current.Adapter;
    public ICharacteristic? ChatCharacteristic { get; set; }

    public async Task ConnectToDeviceAsync(IDevice device)
    {
        if (!CheckDevice(device)) return;
        try
        {
            await ToastHelper.ShowInfoToast($"Connecting to device {device.Name ?? "Unknown"}");
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
        catch (DeviceConnectionException e)
        {
            await ToastHelper.ShowExceptionToast(e);
        }
        catch (ArgumentNullException e)
        {
            await ToastHelper.ShowExceptionToast(e);
        }
    }

    private bool CheckDevice(IDevice device)
    {
        // NOTE: Previously we were requiring a non-null, non-empty Name.
        // However, after an app restart the discovered device may have a null Name even though it advertises the proper service.
        // Removing the Name check lets us connect to valid devices even when the Name isn’t immediately available.
        if (_adapter.ConnectedDevices.Any(d => d.Id == device.Id))
            return false;
        return true;
    }
}
