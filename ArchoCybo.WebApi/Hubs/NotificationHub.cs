using Microsoft.AspNetCore.SignalR;

namespace ArchoCybo.WebApi.Hubs;

public class NotificationHub : Hub
{
    public async Task NotifyProjectUpdated(Guid projectId)
    {
        await Clients.All.SendAsync("ProjectUpdated", projectId);
    }
}
