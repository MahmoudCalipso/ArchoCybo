using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Domain.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace ArchoCybo.Pages.Projects;

public partial class LivePreview : ComponentBase, IDisposable
{
    [Parameter] public Guid ProjectId { get; set; }

    [Inject] public IRepository<GeneratedProject, BaseFilter> ProjectRepo { get; set; } = null!;

    private string projectName = "Loading...";
    private string imageTag = "";
    private string containerId = "";
    private string containerStatus = "Not Created";
    private string exposedUrl = "";
    private int exposedPort = 0;
    private string logs = "";
    private string operationStatus = "";
    private bool isRunning = false;
    private bool isOperating = false;
    private System.Threading.Timer? logTimer;

    protected override async Task OnInitializedAsync()
    {
        var result = await ProjectRepo.Query()
            .Where(p => p.Id == ProjectId)
            // .Include(p => p.Owner) // Need to make sure Owner is available
            .FirstOrDefaultAsync();

        if (result != null)
        {
            projectName = result.Name;
            // Reconstruct path logic (simplified for demonstration)
            // In a real app, this should be stored or managed by a ProjectManagerService
        }
    }

    private async Task BuildAndRun()
    {
        isOperating = true;
        operationStatus = "Preparing project files...";
        StateHasChanged();

        try
        {
            // 1. Get Project Details for Path
            var project = await ProjectRepo.GetByIdAsync(ProjectId);
            if (!project.Success) return;

            // Reconstruct the folder path used by BackendCodeGeneratorService
            // Path: PROJECT-GEN-AI/<USER-ID>-(USER-Name)/<ProjectName>/Backend
            // For now, we'll use a placeholder or assume the folder structure
            var rootPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "PROJECT-GEN-AI");
            var userFolder = $"{project.Data!.OwnerUserId}-Unknown"; // Simplified for now since Owner is not loaded
            var localPath = System.IO.Path.Combine(rootPath, userFolder, project.Data.Name, "Backend");

            if (!System.IO.Directory.Exists(localPath))
            {
                Snackbar.Add("Generated project folder not found. Please regenerate first.", Severity.Error);
                return;
            }

            // 2. Build Image
            operationStatus = "Building Docker Image (this may take a minute)...";
            StateHasChanged();
            imageTag = await DockerService.BuildImageAsync(ProjectId, localPath);

            // 3. Run Container
            operationStatus = "Starting Container...";
            StateHasChanged();
            var runResult = await DockerService.RunContainerAsync(ProjectId, imageTag);
            containerId = runResult.ContainerId;
            exposedPort = runResult.HostPort;
            
            // 4. Update Status
            isRunning = true;
            var status = await DockerService.GetContainerStatusAsync(containerId);
            containerStatus = status;
            exposedUrl = $"http://localhost:{exposedPort}";
            
            exposedUrl = $"http://localhost:{exposedPort}";

            Snackbar.Add("Project is live!", Severity.Success);
            
            // Start log polling
            StartLogPolling();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Deployment failed: {ex.Message}", Severity.Error);
            operationStatus = $"Error: {ex.Message}";
        }
        finally
        {
            isOperating = false;
            StateHasChanged();
        }
    }

    private async Task Stop()
    {
        if (string.IsNullOrEmpty(containerId)) return;

        isOperating = true;
        operationStatus = "Stopping container...";
        StateHasChanged();

        try
        {
            await DockerService.StopContainerAsync(containerId);
            isRunning = false;
            containerStatus = "Stopped";
            exposedUrl = "";
            exposedPort = 0;
            containerId = "";
            StopLogPolling();
            Snackbar.Add("Container stopped.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Stop failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            isOperating = false;
            StateHasChanged();
        }
    }

    private void StartLogPolling()
    {
        logTimer = new System.Threading.Timer(async _ =>
        {
            if (!string.IsNullOrEmpty(containerId))
            {
                try
                {
                    logs = await DockerService.GetContainerLogsAsync(containerId);
                    await InvokeAsync(StateHasChanged);
                }
                catch { /* Ignore log fetch errors while polling */ }
            }
        }, null, 0, 2000);
    }

    private void StopLogPolling()
    {
        logTimer?.Dispose();
        logTimer = null;
    }

    public void Dispose()
    {
        StopLogPolling();
    }
}
