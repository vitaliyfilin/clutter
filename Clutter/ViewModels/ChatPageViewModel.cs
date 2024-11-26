using System.Collections.ObjectModel;
using System.Text;
using Clutter.Models;
using Clutter.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace Clutter.ViewModels;

public sealed partial class ChatPageViewModel : BasePageViewModel
{
    private static readonly Guid _myUuidSecure = Guid.Parse("fa87c0d0-afac-11de-8a39-0800200c9a66");

    private readonly IBluetoothService _bluetoothService;
    private List<IDevice> Devices { get; }
    private readonly IAdapter _adapter;
    private ICharacteristic? _chatCharacteristic;

    [ObservableProperty] private string? _newMessage;
    [ObservableProperty] private ObservableCollection<MessageModel>? _messages;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private bool? _isSendingMessage;
    [ObservableProperty] private double _progress;

    private List<string> _buffer = new();
    private StringBuilder _combinedMessage = new();
    private int MaxChunkSize; // Max BLE chunk size is usually 20 bytes

    public ChatPageViewModel(
        IBluetoothService bluetoothService)
    {
        Devices = [];
        _bluetoothService = bluetoothService;
        _bluetoothService.MessageReceived += OnMessageReceived;

        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceConnectionLost;

        Messages = new ObservableCollection<MessageModel>();
        ScanDevicesPeriodically(new CancellationToken());
    }

    [ICommand]
    private async void ScanDevicesPeriodically(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ScanDevices(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error during scanning: {ex.Message}");
                // Continue scanning even after an error
            }

            try
            {
                // Delay for 10 seconds, unless cancellation is requested
                await Task.Delay(10_000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Gracefully exit the loop if the user cancels
                break;
            }
        }
    }


    private async Task ScanDevices(CancellationToken cancellationToken)
    {
        _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        await _adapter.StartScanningForDevicesAsync(new ScanFilterOptions
            {
                ServiceUuids = [_myUuidSecure]
            }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        var disconnectedDevice = e.Device;
        Devices.Remove(disconnectedDevice);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Messages.Add(new MessageModel
            {
                Content = $"{disconnectedDevice.Name} disconnected.",
                IsSystemMessage = true,
                IsIncoming = null,
                Timestamp = null
            });
        });

        if (disconnectedDevice.IsConnectable)
        {
            await ReconnectToDeviceAsync(disconnectedDevice);
        }
    }

    private async void OnDeviceConnectionLost(object? sender, DeviceEventArgs e)
    {
        var disconnectedDevice = e.Device;
        Devices.Remove(disconnectedDevice);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Messages.Add(new MessageModel
            {
                Content = $"{disconnectedDevice.Name} connection lost.",
                IsSystemMessage = true,
                IsIncoming = null,
                Timestamp = null
            });
        });

        if (disconnectedDevice.IsConnectable)
        {
            await ReconnectToDeviceAsync(disconnectedDevice);
        }
    }

    private async void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
    {
        var device = args.Device;

        if (Devices.Any(d => d.Id == device.Id)) return;

        try
        {
            await ConnectToDeviceAsync(device);
            Devices.Add(device);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(new MessageModel
                {
                    Content = $"{device.Name} connected.",
                    IsSystemMessage = true,
                    IsIncoming = null,
                    Timestamp = null
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to discovered device: {ex.Message}");
        }
    }


    private async Task ReconnectToDeviceAsync(IDevice device)
    {
        try
        {
            if (device.State == DeviceState.Connected) return;

            await ConnectToDeviceAsync(device);
            await Toast.Make($"Reconnected to device: {device.Name}").Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reconnect to device: {device.Name}. Error: {ex.Message}");
        }
    }

    private async Task ConnectToDevicesAsync()
    {
        foreach (var device in Devices)
        {
            await ConnectToDeviceAsync(device);
        }
    }

    private async Task ConnectToDeviceAsync(IDevice device)
    {
        if (!device.IsConnectable || !device.SupportsIsConnectable) return;
        if (device.State is DeviceState.Connected or DeviceState.Connecting) return;
        if (_adapter.ConnectedDevices.Contains(device)) return;
        if (Devices.Any(d => d.Id == device.Id)) return;

        try
        {
            await Task.Delay(1000);
            await _adapter.ConnectToDeviceAsync(device);

            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                foreach (var characteristic in characteristics)
                {
                    if (!characteristic.CanWrite || !characteristic.CanUpdate) continue;
                    _chatCharacteristic = characteristic;
                    await Toast.Make($"Chat characteristic found: {_chatCharacteristic.Id}").Show();
                    //_chatCharacteristic.ValueUpdated += Characteristic_ValueUpdated;
                    await _chatCharacteristic.StartUpdatesAsync();
                }
            }
        }
        catch (Exception e)
        {
            await Toast.Make(e.Message).Show();
        }
    }

    private async Task<int> RequestMtuAsync(int requestValue)
    {
        foreach (var device in Devices)
        {
            var mtu = await device.RequestMtuAsync(requestValue);
            mtu -= (int)(mtu * 0.05); // Subtract 5% of MTU
            await Toast.Make($"Adjusted MTU size: {mtu}").Show();
            return mtu;
        }

        return 100;
    }

    private async void OnMessageReceived(string message, string deviceAddress)
    {
        // Add the incoming message chunk to the buffer
        _buffer.Add(message);

        // Check if the message ends with '^' (text) or '~' (file)
        if (message.EndsWith('^'))
        {
            // Text message logic
            foreach (var chunk in _buffer)
            {
                _combinedMessage.Append(chunk);
            }

            var fullMessage = _combinedMessage.ToString().TrimEnd('^'); // Remove the '^' marker
            _buffer.Clear();
            _combinedMessage.Clear();

            // Verify the message source and display it
            // var macAddress = GuidHelper.GuidToMacAddress(Device.Id);
            // if (deviceAddress != macAddress) return;

            Messages.Add(new MessageModel
            {
                Content = fullMessage,
                IsIncoming = true,
                IsSystemMessage = null
            });
        }
    }

    [ICommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMessage)) return;
        if (Devices.Count == 0) return;
        IsSendingMessage = true;

        try
        {
            await ConnectToDevicesAsync();

            var messageBytes = Encoding.UTF8.GetBytes(NewMessage + "^"); // '^' as end marker
            await Send(messageBytes);

            Messages.Add(new MessageModel
            {
                Content = NewMessage,
                IsIncoming = false,
                IsSystemMessage = null
            });

            NewMessage = string.Empty;
        }
        catch (Exception e)
        {
            await Toast.Make(e.Message).Show();
        }
        finally
        {
            IsSendingMessage = false;
        }
    }

    private async Task Send(byte[] fileContents)
    {
        var result = await RequestMtuAsync(255);

        if (fileContents.Length == 0) return;

        var bytesAlreadySent = 0;
        var bytesRemaining = fileContents.Length - bytesAlreadySent;

        while (bytesRemaining > 0)
        {
            var blockLength = Math.Min(bytesRemaining, result);
            var blockView = new byte[blockLength];
            Array.Copy(fileContents, bytesAlreadySent, blockView, 0, blockLength);

            try
            {
                await _chatCharacteristic?.WriteAsync(blockView)!;
                bytesRemaining -= blockLength;

                Progress = (double)bytesAlreadySent / fileContents.Length;

                Console.WriteLine($"File block written - {bytesRemaining} bytes remaining");
                bytesAlreadySent += blockLength;
            }
            catch (Exception e)
            {
                Console.WriteLine($"File block write error with {bytesRemaining} bytes remaining: {e.Message}");
                break;
            }
        }
    }


    // private async void Characteristic_ValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
    // {
    //     await Toast.Make("WENT TO CHARACTERISTIC METHOD").Show();
    //     try
    //     {
    //         var bytes = args.Characteristic.Value;
    //         var receivedMessage = Encoding.Default.GetString(bytes);
    //         var deviceAddress = args.Characteristic.Service.Id.ToString();
    //
    //         if (deviceAddress == Device.NativeDevice.ToString())
    //         {
    //             Messages.Add(new MessageModel
    //             {
    //                 Content = receivedMessage,
    //                 IsIncoming = true
    //             });
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         await Toast.Make(e.Message).Show();
    //     }
    // }
}