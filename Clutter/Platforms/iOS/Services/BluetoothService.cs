namespace Clutter.Services;

public class BluetoothService: IBluetoothService
{
    public event Action<string, string>? MessageReceived;
}