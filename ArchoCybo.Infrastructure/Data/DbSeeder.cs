using ArchoCybo.Domain.Entities.Security;
using ArchoCybo.SharedKernel.Security;

namespace ArchoCybo.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ArchoCyboDbContext db)
    {
        if (!db.Roles.Any())
        {
            var roles = new[]
            {
                new Role { Name = "SuperUser", DisplayName = "Super User", Description = "Full access", IsSystemRole = true },
                new Role { Name = "UserAgent", DisplayName = "User Agent", Description = "Support agents", IsSystemRole = true },
                new Role { Name = "User", DisplayName = "User", Description = "Subscriber", IsSystemRole = true }
            };
            db.Roles.AddRange(roles);
        }

        if (!db.Permissions.Any())
        {
            var perms = new[]
            {
                new Permission { Name = "projects.fullAccess", DisplayName = "Full Access", Resource = "Projects", Action = "FullAccess" },
                new Permission { Name = "projects.view", DisplayName = "View Projects", Resource = "Projects", Action = "View" },
                new Permission { Name = "projects.generate", DisplayName = "Generate Project", Resource = "Projects", Action = "Generate" },
                new Permission { Name = "users.manage", DisplayName = "Manage Users", Resource = "Users", Action = "Manage" }
            };
            db.Permissions.AddRange(perms);
            await db.SaveChangesAsync();
        }

        // Assign base role permissions
        var superUserRole = db.Roles.FirstOrDefault(r => r.Name == "SuperUser");
        var userAgentRole = db.Roles.FirstOrDefault(r => r.Name == "UserAgent");
        var userRole = db.Roles.FirstOrDefault(r => r.Name == "User");
        var permView = db.Permissions.FirstOrDefault(p => p.Name == "projects.view");
        var permGenerate = db.Permissions.FirstOrDefault(p => p.Name == "projects.generate");
        var permUsersManage = db.Permissions.FirstOrDefault(p => p.Name == "users.manage");
        var permFull = db.Permissions.FirstOrDefault(p => p.Name == "projects.fullAccess");

        if (!db.RolePermissions.Any())
        {
            var rp = new List<RolePermission>();
            if (superUserRole != null)
            {
                if (permFull != null) rp.Add(new RolePermission { RoleId = superUserRole.Id, PermissionId = permFull.Id });
                if (permView != null) rp.Add(new RolePermission { RoleId = superUserRole.Id, PermissionId = permView.Id });
                if (permGenerate != null) rp.Add(new RolePermission { RoleId = superUserRole.Id, PermissionId = permGenerate.Id });
                if (permUsersManage != null) rp.Add(new RolePermission { RoleId = superUserRole.Id, PermissionId = permUsersManage.Id });
            }
            if (userAgentRole != null)
            {
                if (permView != null) rp.Add(new RolePermission { RoleId = userAgentRole.Id, PermissionId = permView.Id });
            }
            // 'User' role intentionally has no permissions to restrict access
            if (rp.Count > 0)
            {
                db.RolePermissions.AddRange(rp);
                await db.SaveChangesAsync();
            }
        }

        // Link discovered endpoints to a global API permission and grant to SuperUser
        var apiAll = db.Permissions.FirstOrDefault(p => p.Name == "api.all");
        if (apiAll == null)
        {
            apiAll = new Permission
            {
                Name = "api.all",
                DisplayName = "All API Endpoints",
                Description = "Grants access to all authenticated API endpoints",
                Category = "API",
                Resource = "API",
                Action = "All",
                Type = ArchoCybo.Domain.Enums.PermissionType.API,
                IsSystemPermission = true
            };
            db.Permissions.Add(apiAll);
            await db.SaveChangesAsync();
        }

        var protectedEndpoints = db.EndpointPermissions.Where(ep => ep.RequiresAuthentication).ToList();
        foreach (var ep in protectedEndpoints)
        {
            if (ep.RequiredPermissionId == null)
            {
                ep.RequiredPermissionId = apiAll.Id;
            }
        }
        if (protectedEndpoints.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        // Drop all users and related links to ensure a clean state
        var allUserIds = db.Users.Select(u => u.Id).ToList();
        if (allUserIds.Count > 0)
        {
            db.UserSessions.RemoveRange(db.UserSessions.Where(us => allUserIds.Contains(us.UserId)));
            db.UserRoles.RemoveRange(db.UserRoles.Where(ur => allUserIds.Contains(ur.UserId)));
            db.UserPermissions.RemoveRange(db.UserPermissions.Where(up => allUserIds.Contains(up.UserId)));
            db.Users.RemoveRange(db.Users.Where(u => allUserIds.Contains(u.Id)));
            await db.SaveChangesAsync();
        }

        var admin = new User
        {
            Email = "admin@archocybo.local",
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("ChangeMe123!"),
            IsActive = true,
            EmailConfirmed = true,
            FailedLoginAttempts = 0,
            LockoutEnd = null,
            FirstName = "System",
            LastName = "Administrator",
            PhoneNumber = "+0000000000"
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var superRole = db.Roles.First(r => r.Name == "SuperUser");
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = superRole.Id });
        await db.SaveChangesAsync();

        // Directly grant global API permission to admin (in addition to role)
        db.UserPermissions.Add(new UserPermission { UserId = admin.Id, PermissionId = apiAll.Id });
        await db.SaveChangesAsync();
    }
}
