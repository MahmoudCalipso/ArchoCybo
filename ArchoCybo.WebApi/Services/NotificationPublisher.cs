using ArchoCybo.Application.Interfaces.IServices;
using Microsoft.AspNetCore.SignalR;
using ArchoCybo.WebApi.Hubs;

namespace ArchoCybo.WebApi.Services;

public class NotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationPublisher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task PublishProjectUpdatedAsync(Guid projectId)
    {
        await _hub.Clients.All.SendAsync("ProjectUpdated", projectId);
    }

    public async Task PublishUserChangedAsync(Guid userId)
    {
        await _hub.Clients.All.SendAsync("UserChanged", userId);
    }
}
