using ArchoCybo.Application.Services.Generation;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces.IServices.Background;

namespace ArchoCybo.WebApi.Services;

public class HangfireJobService
{
    private readonly IUnitOfWork _uow;
    private readonly ProjectGeneratorService _generator;
    private readonly INotificationPublisher _publisher;

    public HangfireJobService(IUnitOfWork uow, ProjectGeneratorService generator, INotificationPublisher publisher)
    {
        _uow = uow;
        _generator = generator;
        _publisher = publisher;
    }

    // This method will be invoked by Hangfire
    public async Task RunGeneration(Guid projectId, Guid triggeredByUserId)
    {
        // Create background job record
        var job = new BackgroundJob
        {
            ProjectId = projectId,
            TriggeredByUserId = triggeredByUserId,
            Status = BackgroundJobStatus.Processing,
            StartedAt = DateTime.UtcNow
        };

        await _uow.Repository<BackgroundJob>().AddAsync(job);
        await _uow.SaveChangesAsync();

        try
        {
            // Generate project files and zip
            var zipPath = await _generator.GenerateAsync(projectId);

            // Update GeneratedProject entity status
            var projRepo = _uow.Repository<GeneratedProject>();
            var project = await projRepo.GetByIdAsync(projectId);
            if (project != null)
            {
                project.Status = ArchoCybo.Domain.Enums.ProjectStatus.Generated;
                project.GeneratedAt = DateTime.UtcNow;
                // store artifact path in GenerationOptions as JSON
                var meta = new { ArtifactZip = zipPath };
                project.GenerationOptions = System.Text.Json.JsonSerializer.Serialize(meta);
                projRepo.Update(project);
            }

            job.Status = BackgroundJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();

            // notify clients
            await _publisher.PublishProjectUpdatedAsync(projectId);
        }
        catch (Exception ex)
        {
            job.Status = BackgroundJobStatus.Failed;
            job.LastError = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            // rethrow so Hangfire records failure
            throw;
        }
    }
}
