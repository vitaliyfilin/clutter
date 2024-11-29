using System.Collections.Concurrent;
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
    private static readonly List<string> ConnectedAlternatives =
    [
        "Decided to grace us with their presence",
        "Made a cameo appearance",
        "Beamed into our orbit",
        "Tapped into the collective",
        "Stepped into the void",
        "Wandered into the party",
    ];

    private static readonly List<string> DisconnectedAlternatives =
    [
        "Decided the grass was greener elsewhere",
        "Ghosted us all",
        "Pulled the plug on the fun",
        "Left without leaving a forwarding address",
        "Evacuated the chat premises",
        "Took an unannounced leave of absence",
    ];

    private static readonly Guid MyUuidSecure = Guid.Parse("fa87c0d0-afac-11de-8a39-0800200c9a66");
    private readonly ConcurrentDictionary<string, StringBuilder> _deviceMessageBuffers = new();
    private const string Me = "Me";
    private HashSet<IDevice> Devices { get; }
    private readonly IAdapter _adapter;

    [ObservableProperty] private string? _newMessage;
    [ObservableProperty] private ObservableCollection<MessageModel>? _messages;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private bool? _isSendingMessage;

    private readonly IBluetoothService _bluetoothService;
    private readonly IConnectionService _connectionService;
    private readonly IMessagingService _messagingService;
    private readonly ISoundService _soundService;

    public ChatPageViewModel(
        IBluetoothService bluetoothService, IConnectionService connectionService, IMessagingService messagingService, ISoundService soundService)
    {
        Devices = [];
        Messages = new ObservableCollection<MessageModel>();
        _bluetoothService = bluetoothService;
        _connectionService = connectionService;
        _messagingService = messagingService;
        _soundService = soundService;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _bluetoothService.MessageReceived += OnMessageReceived;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceDisconnected;

        ScanDevicesPeriodically(new CancellationToken());
    }

    private void OnCharacteristicValueUpdated(object? o, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
    {
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


    [ICommand]
    private async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        IsSendingMessage = true;
        try
        {
            if (NewMessage is null) return;

            var resultList = await _messagingService.SendToDevicesAsync(Devices, NewMessage);

            foreach (var (meta, result, message) in resultList)
            {
                if (!result)
                {
                    await Toast.Make($"Delivery failed for {meta.Name} with error {message}").Show(cancellationToken);
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages?.Add(new MessageModel
                {
                    Name = Me,
                    Content = NewMessage,
                    IsIncoming = false,
                    IsSystemMessage = false,
                    IsAvatarVisible = true
                });
            });

            NewMessage = string.Empty;
        }
        catch (Exception e)
        {
            await Toast.Make($"Exception: {e}, {e.Message}").Show(cancellationToken);
        }
        finally
        {
            IsSendingMessage = false;
            await Task.CompletedTask;
        }
    }

    private async Task ScanDevices(CancellationToken cancellationToken)
    {
        _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        await _adapter.StartScanningForDevicesAsync(new ScanFilterOptions
            {
                ServiceUuids = [MyUuidSecure]
            }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        var disconnectedDevice = e.Device;
        if (disconnectedDevice.State is not DeviceState.Connecting or DeviceState.Connected)
        {
            Devices.Remove(disconnectedDevice);
            _deviceMessageBuffers.TryRemove(GuidHelper.GuidToMacAddress(disconnectedDevice.Id),
                out _); // Cleanup buffer
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Messages?.Add(new MessageModel
            {
                Name = e.Device.Name,
                Content = $"{string.Join(",", Devices.Select(x=>x.Id))}",
                IsSystemMessage = true,
                IsIncoming = null,
                Timestamp = null,
                IsAvatarVisible = false
            });
            
            Messages?.Add(new MessageModel
            {
                Name = e.Device.Name,
                Content = $"{disconnectedDevice.Name} {GetRandomRemark(DisconnectedAlternatives)}.",
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
            await _connectionService.ConnectToDeviceAsync(device);
            if (device.State == DeviceState.Connected)
            {
                Devices.Add(device);
            }

            await _soundService.PlayDiscoveredSoundAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages?.Add(new MessageModel
                {
                    Name = device.Name,
                    Content = $"{string.Join(",", Devices.Select(x=>x.Id))}",
                    IsSystemMessage = true,
                    IsIncoming = null,
                    Timestamp = null,
                    IsAvatarVisible = false
                });
                
                Messages?.Add(new MessageModel
                {
                    Name = device.Name,
                    Content = $"{device.Name} {GetRandomRemark(ConnectedAlternatives)}.",
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

    private static string GetRandomRemark(List<string> list)
    {
        var random = new Random();
        var randomValue = random.Next(0, list.Count);
        return list[randomValue];
    }


    private async void OnMessageReceived(string message, string deviceAddress)
    {
        try
        {
            var endMark = "^";

            if (!_deviceMessageBuffers.TryGetValue(deviceAddress, out var value))
            {
                value = new StringBuilder();
                _deviceMessageBuffers[deviceAddress] = value;
            }

            var buffer = value;
            buffer.Append(message);

            if (!message.EndsWith(endMark)) return;

            var fullMessage = buffer.ToString().Replace(endMark, string.Empty).TrimEnd();
            buffer.Clear();

            var device = Devices.FirstOrDefault(x => GuidHelper.GuidToMacAddress(x.Id) == deviceAddress);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages?.Add(new MessageModel
                {
                    Name = device?.Name,
                    Content = $"{string.Join(",", Devices.Select(x=>x.Id))}",
                    IsSystemMessage = true,
                    IsIncoming = null,
                    Timestamp = null,
                    IsAvatarVisible = false
                });
                
                Messages?.Add(new MessageModel
                {
                    Name = device?.Name,
                    Content = fullMessage,
                    IsIncoming = true,
                    IsSystemMessage = false,
                    IsAvatarVisible = true
                });
            });
            await _soundService.PlayReceivedMessageSoundAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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