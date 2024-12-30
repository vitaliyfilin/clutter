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
            await ToastHelper.ShowInfoToast($"Connecting to device {device.Name}");
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
        if (device.Name is null) return false;
        if (string.Empty.Equals(device.Name)) return false;
        if (_adapter.ConnectedDevices.Any(d => d.Id == device.Id)) return false;
        return !string.IsNullOrWhiteSpace(device.Name);
    }
}