namespace ArchoCybo.Application.Interfaces.IServices;

public interface IDockerService
{
    /// <summary>
    /// Builds a Docker image for the specified project
    /// </summary>
    Task<string> BuildImageAsync(Guid projectId, string localPath);

    /// <summary>
    /// Starts a container for the specified project
    /// </summary>
    Task<ContainerRunResult> RunContainerAsync(Guid projectId, string imageName);

    /// <summary>
    /// Stops and removes the specified container
    /// </summary>
    Task StopContainerAsync(string containerId);

    /// <summary>
    /// Gets the logs from a running container
    /// </summary>
    Task<string> GetContainerLogsAsync(string containerId);

    /// <summary>
    /// Gets the status of a container
    /// </summary>
    Task<string> GetContainerStatusAsync(string containerId);
}
