using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Domain.Entities.Security;
using ArchoCybo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArchoCybo.WebApi.Filters;

public class DynamicPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DynamicPermissionFilter> _logger;

    public DynamicPermissionFilter(IServiceScopeFactory scopeFactory, ILogger<DynamicPermissionFilter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip if [AllowAnonymous] is present
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em is AllowAnonymousAttribute))
        {
            return;
        }

        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor == null) return;

        var controllerName = descriptor.ControllerName;
        var actionName = descriptor.ActionName;
        var method = context.HttpContext.Request.Method;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ArchoCyboDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Find permission requirement for this endpoint
        // We match by Controller/Action first, as it's more reliable than path
        var endpointPerm = await db.EndpointPermissions
            .Include(ep => ep.RequiredPermission)
            .FirstOrDefaultAsync(ep => 
                ep.Controller == controllerName && 
                ep.Action == actionName && 
                ep.HttpMethod == method);

        if (endpointPerm == null)
        {
            // If no explicit permission is defined, we might default to requiring authentication
            // or allow it. For security, let's assume if it's not defined, it's open OR require at least auth.
            // But usually, we only enforce what's in the DB. 
            // If we want "secure by default", we should block. 
            // For now, let's just proceed if not defined (or check if user is authenticated at least).
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                // context.Result = new UnauthorizedResult();
                // We leave it to standard [Authorize] attribute if present.
                // If we want to enforce auth for EVERYTHING, uncomment above.
            }
            return;
        }

        if (endpointPerm.IsPublic)
        {
            return;
        }

        if (endpointPerm.RequiresAuthentication)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        if (endpointPerm.RequiredPermissionId.HasValue)
        {
            var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if user has the permission
            var userPermissions = await userService.GetUserPermissionsAsync(userId);
            var requiredPermName = endpointPerm.RequiredPermission!.Name;

            // Also check for SuperUser role via claims if available, or fetch roles
            var isInSuperUserRole = context.HttpContext.User.IsInRole("SuperUser");
            if (isInSuperUserRole) return; // SuperUser bypass

            if (!userPermissions.Contains(requiredPermName))
            {
                _logger.LogWarning($"User {userId} denied access to {controllerName}/{actionName}. Missing permission: {requiredPermName}");
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
