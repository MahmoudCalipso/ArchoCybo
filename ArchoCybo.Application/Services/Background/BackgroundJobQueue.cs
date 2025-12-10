using System.Threading.Channels;
using ArchoCybo.Application.Interfaces.IServices.Background;

namespace ArchoCybo.Application.Services.Background;

public class BackgroundJobQueue : IBackgroundJobQueue, IDisposable
{
    private readonly Channel<GenerationJob> _queue;

    public BackgroundJobQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<GenerationJob>(options);
    }

    public async ValueTask QueueBackgroundWorkAsync(GenerationJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<GenerationJob> DequeueAsync(CancellationToken cancellationToken)
    {
        var job = await _queue.Reader.ReadAsync(cancellationToken);
        return job;
    }

    public void Dispose() => _queue.Writer.Complete();
}
