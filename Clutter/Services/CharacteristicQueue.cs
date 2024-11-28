using System.Threading.Channels;

namespace Clutter.Services;

public sealed class CharacteristicQueue
{
    private readonly Channel<Func<Task>> _channel = Channel.CreateUnbounded<Func<Task>>();

    public CharacteristicQueue()
    {
        Task.Run(async () => await ProcessQueueAsync());
    }

    public async Task EnqueueRequest(Func<Task> request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var request in _channel.Reader.ReadAllAsync())
        {
            try
            {
                await request();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}