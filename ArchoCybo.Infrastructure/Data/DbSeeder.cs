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

        if (!db.EndpointPermissions.Any())
        {
            var manageUsersPerm = db.Permissions.FirstOrDefault(p => p.Name == "users.manage");
            if (manageUsersPerm != null)
            {
                var endpoints = new[]
                {
                    new EndpointPermission 
                    { 
                        Controller = "Users", 
                        Action = "GetAll", 
                        HttpMethod = "GET", 
                        EndpointPath = "api/Users",
                        RequiresAuthentication = true,
                        RequiredPermissionId = manageUsersPerm.Id
                    },
                    new EndpointPermission 
                    { 
                        Controller = "Users", 
                        Action = "Create", 
                        HttpMethod = "POST", 
                        EndpointPath = "api/Users",
                        RequiresAuthentication = true,
                        RequiredPermissionId = manageUsersPerm.Id
                    },
                    new EndpointPermission 
                    { 
                        Controller = "Users", 
                        Action = "Update", 
                        HttpMethod = "PUT", 
                        EndpointPath = "api/Users",
                        RequiresAuthentication = true,
                        RequiredPermissionId = manageUsersPerm.Id
                    },
                    new EndpointPermission 
                    { 
                        Controller = "Users", 
                        Action = "Delete", 
                        HttpMethod = "DELETE", 
                        EndpointPath = "api/Users/{id}",
                        RequiresAuthentication = true,
                        RequiredPermissionId = manageUsersPerm.Id
                    }
                };
                db.EndpointPermissions.AddRange(endpoints);
                await db.SaveChangesAsync();
            }
        }

        if (!db.Users.Any())
        {
            var admin = new User { Email = "admin@archocybo.local", Username = "admin", PasswordHash = PasswordHasher.Hash("ChangeMe123!"), IsActive = true };
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            // assign SuperUser role
            var superRole = db.Roles.First(r => r.Name == "SuperUser");
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = superRole.Id });
        }

        await db.SaveChangesAsync();
    }
}
