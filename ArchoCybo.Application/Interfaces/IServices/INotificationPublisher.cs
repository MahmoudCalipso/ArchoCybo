namespace ArchoCybo.Application.Interfaces.IServices;

public interface INotificationPublisher
{
    Task PublishProjectUpdatedAsync(Guid projectId);
    Task PublishUserChangedAsync(Guid userId);
}
