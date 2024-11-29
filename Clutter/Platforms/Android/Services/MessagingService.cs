using Clutter.Services.Queues;
using Plugin.BLE.Abstractions.Contracts;
using static System.Buffers.ArrayPool<byte>;
using static System.Text.Encoding;

namespace Clutter.Services;

public sealed class MessagingService : IMessagingService
{
    private const int MaxBleMtuSize = 512;
    private const int RealisticBleMtuSize = 20;
    private const string MessageEndMarker = "^";
    private readonly CharacteristicQueue _characteristicQueue;
    private readonly IConnectionService _connectionService;

    public MessagingService(IConnectionService connectionService)
    {
        _connectionService = connectionService;
        _characteristicQueue = new CharacteristicQueue();
    }

    public async Task<List<(IDevice Device, bool Success, string? ErrorMessage)>> SendToDevicesAsync(
        HashSet<IDevice> devices, string message)
    {
        var taskCompletionSource =
            new TaskCompletionSource<List<(IDevice Device, bool Success, string? ErrorMessage)>>();

        await _characteristicQueue.EnqueueRequest(async () =>
        {
            var results = await SendAsync(devices, message);
            taskCompletionSource.SetResult(results);
        });

        return await taskCompletionSource.Task;
    }

    private async Task<List<(IDevice Device, bool Success, string? ErrorMessage)>> SendAsync(
        HashSet<IDevice> devices, string message)
    {
        var results = new List<(IDevice Device, bool Success, string? ErrorMessage)>();

        foreach (var device in devices)
        {
            bool success;
            string? errorMessage = null;

            try
            {
                await _connectionService.ConnectToDeviceAsync(device);

                var result = await RequestMtuAsync(device, MaxBleMtuSize);

                await SendAsync(result, message);

                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }

            results.Add((device, success, errorMessage));
        }

        return results;
    }

    private static async Task<int> RequestMtuAsync(IDevice device, int requestValue)
    {
        var requestedMtu = await device.RequestMtuAsync(requestValue);
        requestedMtu -= (int)(requestedMtu * 0.05);

        return requestedMtu > decimal.Zero
            ? requestedMtu
            : RealisticBleMtuSize;
    }

    private async Task SendAsync(int mtuSize, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var fileContents = UTF8.GetBytes(message + MessageEndMarker);

        var bytesRemaining = fileContents.Length;
        var bytesAlreadySent = 0;

        var buffer = Shared.Rent(mtuSize);

        try
        {
            while (bytesRemaining > 0)
            {
                var blockLength = Math.Min(bytesRemaining, mtuSize);

                if (blockLength <= 0)
                {
                    Console.WriteLine("Block length is invalid: " + blockLength);
                    break;
                }

                Buffer.BlockCopy(fileContents, bytesAlreadySent, buffer, 0, blockLength);

                try
                {
                    var characteristic = _connectionService.ChatCharacteristic;
                    await characteristic?.WriteAsync(buffer.AsMemory(0, blockLength).ToArray())!;

                    bytesRemaining -= blockLength;
                    bytesAlreadySent += blockLength;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error with block write: {e.Message} | Bytes remaining: {bytesRemaining}");
                    break;
                }
            }
        }
        finally
        {
            Shared.Return(buffer);
        }
    }
}