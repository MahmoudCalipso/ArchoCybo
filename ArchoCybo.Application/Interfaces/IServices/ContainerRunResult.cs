namespace ArchoCybo.Application.Interfaces.IServices;

public class ContainerRunResult
{
    public string ContainerId { get; set; } = string.Empty;
    public int HostPort { get; set; }
}
