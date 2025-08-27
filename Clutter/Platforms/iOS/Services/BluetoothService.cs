namespace Clutter.Services;

public class BluetoothService: IBluetoothService
{
    public event Action<string, string>? MessageReceived;
    public List<string?> GetConnectedDevices()
    {
        throw new NotImplementedException();
    }
}