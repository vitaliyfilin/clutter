namespace Clutter.Services;

public interface IBluetoothService
{
    event Action<string, string> MessageReceived;
    List<string?> GetConnectedDevices();
}