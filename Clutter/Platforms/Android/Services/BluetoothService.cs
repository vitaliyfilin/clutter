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
        StopAdvertisingAndDispose();

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
        var bluetoothAdapter = _bluetoothManager?.Adapter;
        _bluetoothLeAdvertiser = bluetoothAdapter?.BluetoothLeAdvertiser;

        bluetoothAdapter?.SetName("Daria");

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
    
    
    public void StopAdvertisingAndDispose()
    {
        try
        {
            // Stop advertising
            _bluetoothLeAdvertiser?.StopAdvertising(new MyAdvertiseCallback());

            // Close the GATT server
            _bluetoothGattServer?.ClearServices();
            _bluetoothGattServer?.Close();

            // Nullify the references to allow garbage collection
            _bluetoothGattServer = null;
            _bluetoothGattCharacteristic = null;
            _bluetoothGattService = null;
            _bluetoothLeAdvertiser = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Bluetooth cleanup: {ex.Message}");
        }
    }

    public List<string?> GetConnectedDevices()
    {
        return _gattServerCallback.ConnectedDevices
            .Select(device => device.Address)
            .ToList();
    }

    // GATT server callback to handle incoming write requests and notifications
    private sealed class GattServerCallback : BluetoothGattServerCallback
    {
        private readonly BluetoothService _bluetoothService;
        private readonly HashSet<BluetoothDevice> _connectedDevices = new(); // Track connected devices
        public IEnumerable<BluetoothDevice> ConnectedDevices => _connectedDevices;

        public GattServerCallback(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService;
        }

        public override void OnConnectionStateChange(BluetoothDevice? device, ProfileState status,
            ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                _connectedDevices.Add(device);
                // Initialize message list for the connected device
                if (!_bluetoothService._deviceMessages.ContainsKey(device.Address))
                {
                    _bluetoothService._deviceMessages[device.Address] = new List<string>();
                }
            }
            else if (newState == ProfileState.Disconnected)
            {
                _connectedDevices.Remove(device);
                _bluetoothService._deviceMessages.Remove(device.Address); // Clean up message history
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
            try
            {
                if (characteristic?.Uuid != ChatCharacteristicUuid || device == null || value == null)
                    return;

                var receivedMessage = Encoding.UTF8.GetString(value);

                // Notify UI or relevant listeners
                _bluetoothService.MessageReceived?.Invoke(receivedMessage, device.Address);

                // Broadcast message to all connected devices except the sender
                foreach (var targetDevice in _connectedDevices)
                {
                    if (targetDevice.Address == device.Address) continue; // Skip the sender
#pragma warning disable CA1422
                    characteristic?.SetValue(value);
                    _bluetoothService._bluetoothGattServer?.NotifyCharacteristicChanged(targetDevice, characteristic,
                        false);
#pragma warning restore CA1422
                }

                if (responseNeeded)
                {
                    _bluetoothService._bluetoothGattServer?.SendResponse(device, requestId, GattStatus.Success, 0,
                        value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}