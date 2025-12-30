using ArchoCybo.Application.Interfaces.IServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ArchoCybo.Application.Services.Docker;

public class DockerService : IDockerService
{
    private readonly DockerClient _client;
    private readonly ILogger<DockerService> _logger;

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
        // Connect to local Docker daemon (Windows/CI usually works with this)
        _client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
    }

    public async Task<string> BuildImageAsync(Guid projectId, string localPath)
    {
        _logger.LogInformation("Building image for project {ProjectId} at {Path}", projectId, localPath);
        
        // This is a simplified version. Real implementation would stream logs.
        // For MVP, we use the project name as tag
        var projectName = Path.GetFileName(localPath.TrimEnd(Path.DirectorySeparatorChar)).ToLower();
        var tag = $"archocybo-{projectName}:latest";

        using (var stream = await CreateTarball(localPath))
        {
            await _client.Images.BuildImageFromDockerfileAsync(stream, new ImageBuildParameters
            {
                Dockerfile = "Dockerfile",
                Tags = new List<string> { tag }
            }, CancellationToken.None);
        }

        return tag;
    }

    public async Task<ContainerRunResult> RunContainerAsync(Guid projectId, string imageName)
    {
        _logger.LogInformation("Running container for project {ProjectId} with image {Image}", projectId, imageName);

        var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = imageName,
            Name = $"archocybo-{projectId}-{DateTime.UtcNow.Ticks}", // Unique name
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "80/tcp", new EmptyStruct() }
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "80/tcp", new List<PortBinding> { new PortBinding { HostPort = "0" } } } // Dynamic port
                }
            }
        });

        await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());

        // Inspect to get the host port
        var inspect = await _client.Containers.InspectContainerAsync(response.ID);
        var hostPortStr = inspect.NetworkSettings.Ports["80/tcp"]?[0]?.HostPort ?? "0";
        int.TryParse(hostPortStr, out var hostPort);

        return new ContainerRunResult
        {
            ContainerId = response.ID,
            HostPort = hostPort
        };
    }

    public async Task StopContainerAsync(string containerId)
    {
        await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
        await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
    }

    public async Task<string> GetContainerLogsAsync(string containerId)
    {
        var result = await _client.Containers.GetContainerLogsAsync(containerId, false, new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Tail = "100"
        });

        var (stdout, stderr) = await result.ReadOutputToEndAsync(CancellationToken.None);
        return stdout + stderr;
    }

    public async Task<string> GetContainerStatusAsync(string containerId)
    {
        var response = await _client.Containers.InspectContainerAsync(containerId);
        return response.State.Status;
    }

    private async Task<Stream> CreateTarball(string folderPath)
    {
        var ms = new MemoryStream();
        await using (var tarWriter = new System.Formats.Tar.TarWriter(ms, System.Formats.Tar.TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(folderPath, file).Replace('\\', '/');
                await tarWriter.WriteEntryAsync(file, relativePath);
            }
        }
        ms.Position = 0;
        return ms;
    }
}
