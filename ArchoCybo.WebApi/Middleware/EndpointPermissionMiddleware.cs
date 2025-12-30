using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ArchoCybo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.WebApi.Middleware;

public class EndpointPermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EndpointPermissionMiddleware> _logger;

    public EndpointPermissionMiddleware(RequestDelegate next, ILogger<EndpointPermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        // Skip if endpoint is not mapped to a controller/action
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // Skip static files, swagger, hangfire, etc.
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger") || path.StartsWith("/hangfire") || path.StartsWith("/hubs"))
        {
            await _next(context);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArchoCyboDbContext>();

        // Find endpoint record
        var method = context.Request.Method.ToUpper();
        var ep = await db.EndpointPermissions.FirstOrDefaultAsync(x => x.HttpMethod == method && (x.EndpointPath == context.Request.Path || (x.EndpointPath != null && context.Request.Path.StartsWithSegments(x.EndpointPath))));

        if (ep == null)
        {
            // If not found, allow (or choose deny by default). We'll allow for flexibility.
            await _next(context);
            return;
        }

        if (ep.IsPublic)
        {
            await _next(context);
            return;
        }

        if (ep.RequiresAuthentication && !context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
            return;
        }

        if (ep.RequiredPermissionId.HasValue)
        {
            // Check user permissions (direct or via roles)
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Forbidden" });
                return;
            }

            var userId = Guid.Parse(userIdClaim);
            var has = await db.UserPermissions.AnyAsync(up => up.UserId == userId && up.PermissionId == ep.RequiredPermissionId.Value) ||
                      await db.UserRoles.Include(ur => ur.Role).ThenInclude(r => r.RolePermissions)
                        .Where(ur => ur.UserId == userId)
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .AnyAsync(rp => rp.PermissionId == ep.RequiredPermissionId.Value);

            if (!has)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Permission denied" });
                return;
            }
        }

        await _next(context);
    }
}
