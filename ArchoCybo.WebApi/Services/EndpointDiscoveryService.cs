using ArchoCybo.Domain.Entities.Security;
using ArchoCybo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.WebApi.Services;

public class EndpointDiscoveryService
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EndpointDiscoveryService> _logger;

    public EndpointDiscoveryService(
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<EndpointDiscoveryService> logger)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task DiscoverEndpointsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArchoCyboDbContext>();

        var endpoints = _actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Select(x => new
            {
                Controller = x.ControllerName,
                Action = x.ActionName,
                Method = x.ActionConstraints?.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.FirstOrDefault() ?? "GET",
                Route = x.AttributeRouteInfo?.Template ?? "",
                IsPublic = x.EndpointMetadata?.Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute) ?? false
            })
            .Distinct()
            .ToList();

        var existing = await db.EndpointPermissions.ToListAsync();
        var newCount = 0;

        foreach (var ep in endpoints)
        {
            var match = existing.FirstOrDefault(x => 
                x.Controller == ep.Controller && 
                x.Action == ep.Action && 
                x.HttpMethod == ep.Method);

            if (match == null)
            {
                db.EndpointPermissions.Add(new EndpointPermission
                {
                    Controller = ep.Controller,
                    Action = ep.Action,
                    HttpMethod = ep.Method,
                    EndpointPath = ep.Route,
                    IsPublic = ep.IsPublic,
                    RequiresAuthentication = !ep.IsPublic,
                    Description = $"Auto-discovered {ep.Method} {ep.Route}"
                });
                newCount++;
            }
            else
            {
                // Update route if changed
                if (match.EndpointPath != ep.Route)
                {
                    match.EndpointPath = ep.Route;
                }
                // Sync public/auth flags
                match.IsPublic = ep.IsPublic;
                match.RequiresAuthentication = !ep.IsPublic;
            }
        }

        // Remove stale endpoints
        var toRemove = existing.Where(ex => !endpoints.Any(ep =>
            ep.Controller == ex.Controller &&
            ep.Action == ex.Action &&
            ep.Method == ex.HttpMethod)).ToList();
        foreach (var stale in toRemove)
        {
            db.EndpointPermissions.Remove(stale);
        }

        if (newCount > 0 || db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
            _logger.LogInformation($"Discovered {newCount} new endpoints. Total endpoints: {endpoints.Count}");
        }
    }
}
