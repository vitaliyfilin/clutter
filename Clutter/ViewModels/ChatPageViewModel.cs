using System.Collections.ObjectModel;
using System.Text;
using Clutter.Helpers;
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
    private HashSet<IDevice> Devices { get; }
    private readonly IAdapter _adapter;
    private ICharacteristic? _chatCharacteristic;

    [ObservableProperty] private string? _newMessage;
    [ObservableProperty] private ObservableCollection<MessageModel>? _messages;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private bool? _isSendingMessage;
    [ObservableProperty] private double _progress;
    private readonly CharacteristicQueue _characteristicQueue;
    private readonly StringBuilder _combinedMessage = new();

    public ChatPageViewModel(
        IBluetoothService bluetoothService)
    {
        Devices = [];
        _characteristicQueue = new CharacteristicQueue();
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
            }

            try
            {
                await Task.Delay(5_000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
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
                Name = e.Device.Name,
                Content = $"{disconnectedDevice.Name} disconnected.",
                IsSystemMessage = true,
                IsIncoming = null,
                Timestamp = null,
                IsAvatarVisible = false
            });
        });
    }

    private async void OnDeviceConnectionLost(object? sender, DeviceEventArgs e)
    {
        var disconnectedDevice = e.Device;
        disconnectedDevice.Dispose();
        Devices.Remove(disconnectedDevice);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Messages.Add(new MessageModel
            {
                Name = e.Device.Name,
                Content = $"{disconnectedDevice.Name} connection lost.",
                IsSystemMessage = true,
                IsIncoming = null,
                Timestamp = null,
                IsAvatarVisible = false
            });
        });
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
                    Name = device.Name,
                    Content = $"{device.Name} connected.",
                    IsSystemMessage = true,
                    IsIncoming = null,
                    Timestamp = null,
                    IsAvatarVisible = false
                });
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to discovered device: {ex.Message}");
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
        if (device.Name is null || string.IsNullOrWhiteSpace(device.Name)) return;

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
            await Toast.Make($"Exception: {e}, {e.Message}").Show();
        }
    }

    private async Task<int> RequestMtuAsync(int requestValue)
    {
        foreach (var device in Devices)
        {
            var mtu = await device.RequestMtuAsync(requestValue);
            mtu -= (int)(mtu * 0.05); // Subtract 5% of MTU
            await Toast.Make($"Adjusted MTU size: {mtu}").Show();
            return mtu > 0 ? mtu : 20;
        }

        return 100;
    }

    private void OnMessageReceived(string message, string deviceAddress)
    {
        var spanMessage = message.AsSpan();
        var endMark = "^".AsSpan();
        var device = Devices.FirstOrDefault(x => GuidHelper.GuidToMacAddress(x.Id) == deviceAddress);

        _combinedMessage.Append(spanMessage);

        if (!spanMessage.EndsWith(endMark)) return;
        var fullMessage = _combinedMessage.ToString().TrimEnd();
        _combinedMessage.Clear();

        // Verify the message source and display it
        // var macAddress = GuidHelper.GuidToMacAddress(Device.Id);
        // if (deviceAddress != macAddress) return;

        Messages.Add(new MessageModel
        {
            Name = device.Name,
            Content = fullMessage,
            IsIncoming = true,
            IsSystemMessage = false,
            IsAvatarVisible = true
        });
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
                Name = "Me",
                Content = NewMessage,
                IsIncoming = false,
                IsSystemMessage = false,
                IsAvatarVisible = true
            });

            NewMessage = string.Empty;
        }
        catch (Exception e)
        {
            await Toast.Make($"Exception: {e}, {e.Message}").Show();
        }
        finally
        {
            IsSendingMessage = false;
        }
    }


    private async Task Send(byte[] fileContents)
    {
        var result = 100;
        await _characteristicQueue.EnqueueRequest(async () => { result = await RequestMtuAsync(512); });

        if (fileContents.Length == 0) return;

        var bytesAlreadySent = 0;
        var bytesRemaining = fileContents.Length;

        while (bytesRemaining > 0)
        {
            // Safeguard: Ensure blockLength is within safe limits
            var blockLength = Math.Min(bytesRemaining, result);

            // Prevent overflow when allocating the byte array
            if (blockLength is <= 0 or > int.MaxValue)
            {
                Console.WriteLine("Block length is invalid: " + blockLength);
                break;
            }

            var blockView = new byte[blockLength];

            try
            {
                Array.Copy(fileContents, bytesAlreadySent, blockView, 0, blockLength);

                await _characteristicQueue.EnqueueRequest(async () =>
                {
                    await _chatCharacteristic.WriteAsync(blockView);
                });

                bytesRemaining -= blockLength;

                // Ensure there's no division by zero in progress calculation
                if (fileContents.Length > 0)
                {
                    Progress = (double)bytesAlreadySent / fileContents.Length;
                }

                Console.WriteLine($"File block written - {bytesRemaining} bytes remaining");
                bytesAlreadySent += blockLength;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error with block write: {e.Message} | Bytes remaining: {bytesRemaining}");
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