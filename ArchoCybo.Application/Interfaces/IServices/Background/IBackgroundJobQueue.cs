using System.Threading.Channels;

namespace ArchoCybo.Application.Interfaces.IServices.Background;

public interface IBackgroundJobQueue
{
    ValueTask QueueBackgroundWorkAsync(GenerationJob job);
    ValueTask<GenerationJob> DequeueAsync(CancellationToken cancellationToken);
}

public record GenerationJob(Guid ProjectId, Guid TriggeredByUserId);
