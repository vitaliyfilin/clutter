using System.Text;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Java.Util;
using Application = Android.App.Application;

namespace Clutter.Services;

public sealed class BluetoothService : IBluetoothService
{
    private const GattProperty Properties = GattProperty.Read | GattProperty.Write | GattProperty.Notify;
    private const GattPermission Permissions = GattPermission.Read | GattPermission.Write;

    private static readonly UUID?
        ChatServiceUuid = UUID.FromString("fa87c0d0-afac-11de-8a39-0800200c9a66");

    private static readonly UUID?
        ChatCharacteristicUuid = UUID.FromString("fa87c0d1-afac-11de-8a39-0800200c9a66");

    public event Action<string, string>? MessageReceived;
    private readonly GattServerCallback _gattServerCallback;
    private readonly BluetoothManager? _bluetoothManager;

    private BluetoothGattCharacteristic? _bluetoothGattCharacteristic;
    private BluetoothGattService? _bluetoothGattService;
    private BluetoothLeAdvertiser? _bluetoothLeAdvertiser;
    private BluetoothGattServer? _bluetoothGattServer;

    private readonly Dictionary<string, List<string>> _deviceMessages = new();

    public BluetoothService()
    {
        _bluetoothManager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService)!;
        _gattServerCallback = new GattServerCallback(bluetoothService: this);

        InitializeGattServer();
        StartAdvertising();
    }

    private void InitializeGattServer()
    {
        _bluetoothGattServer = _bluetoothManager?.OpenGattServer(Application.Context, new GattServerCallback(this));
        _bluetoothGattService = new BluetoothGattService(ChatServiceUuid, GattServiceType.Primary);
        _bluetoothGattCharacteristic =
            new BluetoothGattCharacteristic(ChatCharacteristicUuid, properties: Properties, permissions: Permissions);

        _bluetoothGattService.AddCharacteristic(_bluetoothGattCharacteristic);
        _bluetoothGattServer?.AddService(_bluetoothGattService);
    }

    public async void StartAdvertising()
    {
        await Task.Delay(5_000);

        var bluetoothAdapter = _bluetoothManager?.Adapter;
        _bluetoothLeAdvertiser = bluetoothAdapter?.BluetoothLeAdvertiser;

        var advertisementData = new AdvertiseData.Builder()
            .AddServiceUuid(new ParcelUuid(ChatServiceUuid))
            ?.SetIncludeDeviceName(true)
            ?.Build();

        var settings = new AdvertiseSettings.Builder()
            .SetAdvertiseMode(AdvertiseMode.Balanced)
            ?.SetTxPowerLevel(AdvertiseTx.PowerMedium)
            ?.SetConnectable(true)
            ?.Build();

        _bluetoothLeAdvertiser?.StartAdvertising(settings, advertisementData, new MyAdvertiseCallback());
    }

    public List<string?> GetConnectedDevices()
    {
        return _gattServerCallback.ConnectedDevices
            .Select(device => device.Address)
            .ToList();
    }

    // GATT server callback to handle incoming write requests and notifications
    private sealed class GattServerCallback(BluetoothService bluetoothService) : BluetoothGattServerCallback
    {
        private readonly HashSet<BluetoothDevice> _connectedDevices = [];
        public IEnumerable<BluetoothDevice> ConnectedDevices => _connectedDevices;

        public override void OnConnectionStateChange(BluetoothDevice? device, ProfileState status,
            ProfileState newState)
        {
            switch (newState)
            {
                case ProfileState.Connected:
                {
                    if (device != null)
                    {
                        _connectedDevices.Add(device);
                        if (device.Address != null && !bluetoothService._deviceMessages.ContainsKey(device.Address))
                        {
                            bluetoothService._deviceMessages[device.Address] = [];
                        }
                    }

                    break;
                }
                
                case ProfileState.Disconnected:
                    if (device != null)
                    {
                        _connectedDevices.Remove(device);
                        if (device.Address != null)
                            bluetoothService._deviceMessages.Remove(device.Address); 
                    }
                    break;
                case ProfileState.Connecting:
                    break;
                case ProfileState.Disconnecting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        public override void OnCharacteristicWriteRequest(
            BluetoothDevice? device,
            int requestId,
            BluetoothGattCharacteristic? characteristic,
            bool preparedWrite,
            bool responseNeeded,
            int offset,
            byte[]? value)
        {
            if (characteristic?.Uuid != ChatCharacteristicUuid || device == null || value == null ||
                device.Name is null || string.IsNullOrWhiteSpace(device.Name) || device.Address is null ||
                string.IsNullOrWhiteSpace(device.Address))
                return;

            var receivedMessage = Encoding.UTF8.GetString(value);

            if (bluetoothService._deviceMessages.TryGetValue(device.Address, out var message))
            {
                message.Add(receivedMessage);
            }

            bluetoothService.MessageReceived?.Invoke(receivedMessage, device.Address);

            var targetDevice = _connectedDevices.FirstOrDefault(d => d.Address != device.Address);
            if (targetDevice != null)
            {
#pragma warning disable CA1422
                characteristic?.SetValue(value);
                bluetoothService._bluetoothGattServer?.NotifyCharacteristicChanged(targetDevice, characteristic,
                    false);
#pragma warning restore CA1422
            }

            if (responseNeeded)
            {
                bluetoothService._bluetoothGattServer?.SendResponse(device, requestId, GattStatus.Success, 0, value);
            }
        }
    }
}