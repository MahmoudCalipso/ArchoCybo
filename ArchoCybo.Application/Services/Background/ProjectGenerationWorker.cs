using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ArchoCybo.Application.Interfaces.IServices.Background;
using ArchoCybo.Application.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace ArchoCybo.Application.Services.Background;

public class ProjectGenerationWorker : BackgroundService
{
    private readonly IBackgroundJobQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectGenerationWorker> _logger;

    public ProjectGenerationWorker(IBackgroundJobQueue queue, IServiceProvider serviceProvider, ILogger<ProjectGenerationWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectGenerationWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                using var scope = _serviceProvider.CreateScope();
                var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
                _logger.LogInformation("Processing generation job for project {ProjectId}", job.ProjectId);
                await projectService.GenerateProjectAsync(job.ProjectId, job.TriggeredByUserId);
                _logger.LogInformation("Completed generation job for project {ProjectId}", job.ProjectId);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background job");
            }
        }
        _logger.LogInformation("ProjectGenerationWorker stopped");
    }
}
