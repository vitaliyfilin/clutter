using System.Collections.Concurrent;

namespace Clutter.Services.Queues;

public sealed class CharacteristicQueue
{
    private readonly ConcurrentQueue<Func<Task>> _requestQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

    public async Task EnqueueRequest(Func<Task> request)
    {
        _requestQueue.Enqueue(request);
        await ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        if (!await _queueSemaphore.WaitAsync(0))
            return;

        try
        {
            while (_requestQueue.TryDequeue(out var request))
            {
                try
                {
                    await request();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }
        finally
        {
            _queueSemaphore.Release();
        }
    }
}