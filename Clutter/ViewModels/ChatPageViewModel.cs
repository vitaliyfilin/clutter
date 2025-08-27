using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using Clutter.Helpers;
using Clutter.Models;
using Clutter.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using static Plugin.BLE.CrossBluetoothLE;

namespace Clutter.ViewModels;

public sealed partial class ChatPageViewModel : BasePageViewModel
{
    #region Fields and Properties

    private static readonly Guid MyUuidSecure = Guid.Parse("fa87c0d0-afac-11de-8a39-0800200c9a66");
    private readonly ConcurrentDictionary<string, StringBuilder> _deviceMessageBuffers = new();
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

    #endregion

    #region Constructor

    public ChatPageViewModel(
        IBluetoothService bluetoothService,
        IConnectionService connectionService,
        IMessagingService messagingService,
        ISoundService soundService)
    {
        Devices = new HashSet<IDevice>();
        Messages = new ObservableCollection<MessageModel>();
        _bluetoothService = bluetoothService;
        _connectionService = connectionService;
        _messagingService = messagingService;
        _soundService = soundService;
        _adapter = Current.Adapter;

        _bluetoothService.MessageReceived += OnMessageReceived;
        _adapter.DeviceDisconnected += OnDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnDeviceDisconnected;
        _adapter.DeviceDiscovered += OnDeviceDiscovered;

        ScanDevicesPeriodically(new CancellationTokenSource().Token);
    }

    #endregion

    #region Commands

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
                await Task.Delay(10_000, cancellationToken);
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
            if (string.IsNullOrWhiteSpace(NewMessage)) return;

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
                    Name = "Me",
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

    #endregion

    #region Bluetooth Scanning

    private async Task ScanDevices(CancellationToken cancellationToken)
    {
        await _adapter.StartScanningForDevicesAsync(new ScanFilterOptions
                {
                    ServiceUuids = [MyUuidSecure]
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Event Handlers

    private async void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        var disconnectedDevice = e.Device;
        if (disconnectedDevice.State is not DeviceState.Connected)
        {
            await _adapter.DisconnectDeviceAsync(disconnectedDevice);
            Devices.Remove(disconnectedDevice);
            _deviceMessageBuffers.TryRemove(GuidHelper.GuidToMacAddress(disconnectedDevice.Id),
                out _);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Messages?.Add(new MessageModel
            {
                Name = e.Device.Name,
                Content = $"{disconnectedDevice.Name} {GetRandomRemark(ChatHelper.DisconnectedAlternatives)}.",
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

        // First, if this device is already connected via our peripheral role,
        // skip trying to connect as a client.
        // (GuidHelper.GuidToMacAddress converts the Plugin.BLE device Id into the same string
        // format used in our BluetoothService.)
        if (_bluetoothService.GetConnectedDevices().Contains(GuidHelper.GuidToMacAddress(device.Id)))
            return;

        // Also, if we already have it in our Devices set, skip.
        if (Devices.Any(d => d.Id == device.Id))
            return;

        try
        {
            await _connectionService.ConnectToDeviceAsync(device);
            if (device.State == DeviceState.Connected)
            {
                Devices.Add(device);
            }

            // (Additional code for sound/notification remains unchanged)
            await _soundService.PlayDiscoveredSoundAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages?.Add(new MessageModel
                {
                    Name = device.Name,
                    Content = $"{device.Name} {GetRandomRemark(ChatHelper.ConnectedAlternatives)}.",
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


    private async void OnMessageReceived(string message, string deviceAddress)
    {
        try
        {
            const string endMark = "^";

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

    #endregion

    #region Helpers

    private static string GetRandomRemark(List<string> list)
    {
        var random = new Random();
        var randomValue = random.Next(0, list.Count);
        return list[randomValue];
    }

    #endregion
}