using System;
using System.Linq;
using System.Threading.Tasks;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Services;
using ArchoCybo.Domain.Entities.Security;
using ArchoCybo.Infrastructure.Data;
using ArchoCybo.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchoCybo.Tests;

public class UserServiceTests
{
    private ArchoCyboDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ArchoCyboDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ArchoCyboDbContext(options);
    }

    [Fact]
    public async Task UpdateUserDetails_WritesAuditLog()
    {
        using var db = CreateDb();
        var uow = new UnitOfWork(db);
        var svc = new UserService(uow);

        var acting = new User { Username = "admin", Email = "admin@example.com" };
        var target = new User { Username = "user", Email = "user@example.com" };
        db.Users.AddRange(acting, target);
        await db.SaveChangesAsync();

        var dto = new UpdateUserDetailsDto
        {
            Email = "new@example.com",
            Username = "user_new",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "12345",
            Avatar = "http://avatar",
            IsActive = true
        };

        await svc.UpdateUserDetailsAsync(acting.Id, target.Id, dto);

        var updated = await db.Users.FindAsync(target.Id);
        Assert.Equal("new@example.com", updated!.Email);
        Assert.Equal("user_new", updated.Username);

        var audit = await db.AuditLogs.FirstOrDefaultAsync(al => al.EntityId == target.Id.ToString() && al.Action == "Updated");
        Assert.NotNull(audit);
        Assert.Equal(acting.Id, audit!.UserId);
    }

    [Fact]
    public async Task UpdateUserRoles_PreventsEscalation()
    {
        using var db = CreateDb();
        var uow = new UnitOfWork(db);
        var svc = new UserService(uow);

        var roleLow = new Role { Name = "Support", DisplayName = "Support", Priority = 1 };
        var roleHigh = new Role { Name = "Manager", DisplayName = "Manager", Priority = 5 };
        db.Roles.AddRange(roleLow, roleHigh);

        var acting = new User { Username = "actor", Email = "actor@example.com" };
        db.Users.Add(acting);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRole { UserId = acting.Id, RoleId = roleLow.Id });

        var target = new User { Username = "target", Email = "target@example.com" };
        db.Users.Add(target);
        await db.SaveChangesAsync();
        db.UserRoles.Add(new UserRole { UserId = target.Id, RoleId = roleHigh.Id });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await svc.UpdateUserRolesAsync(acting.Id, target.Id, new System.Collections.Generic.List<Guid> { roleLow.Id });
        });
    }
}
