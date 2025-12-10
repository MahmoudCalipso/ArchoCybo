using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.SharedKernel.Security;

namespace ArchoCybo.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> CreateUserAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            IsActive = true
        };
        await _uow.Repository<User>().AddAsync(user);
        await _uow.SaveChangesAsync();
        return user.Id;
    }

    public async Task UpdateUserAsync(UpdateUserDto dto)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(dto.Id);
        if (user == null) throw new Exception("User not found");
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        repo.Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");
        repo.Remove(user);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<UserListData>> GetUsersAsync(UserFilterQuery filter)
    {
        var repo = _uow.Repository<User>();
        var query = repo.Query();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(u => EF.Functions.Like(u.Username.ToLower(), $"%{s}%") || EF.Functions.Like(u.Email.ToLower(), $"%{s}%"));
        }

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .ToListAsync();

        return users.Select(u => new UserListData(u.Id, u.Username, u.Email, u.IsActive,
            u.UserRoles.Select(ur => ur.Role.Name), u.UserPermissions.Select(up => up.Permission.Name))).ToList();
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        var repo = _uow.Repository<User>();
        var user = (await repo.Query().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId))!;

        if (user == null) return Enumerable.Empty<string>();

        var rolePerms = user.UserRoles.SelectMany(ur => ur.Role.RolePermissions).Select(rp => rp.Permission.Name);
        var directPerms = user.UserPermissions.Select(up => up.Permission.Name);
        return rolePerms.Union(directPerms).Distinct();
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId)
    {
        var userRepo = _uow.Repository<User>();
        var roleRepo = _uow.Repository<Domain.Entities.Security.Role>();

        var user = await userRepo.GetByIdAsync(userId);
        var role = await roleRepo.GetByIdAsync(roleId);
        if (user == null || role == null) throw new Exception("User or Role not found");

        var userRole = new Domain.Entities.Security.UserRole { UserId = userId, RoleId = roleId };
        await _uow.Repository<Domain.Entities.Security.UserRole>().AddAsync(userRole);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var repo = _uow.Repository<Domain.Entities.Security.UserRole>();
        var ur = await repo.Query().FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
        if (ur == null) throw new Exception("UserRole not found");
        repo.Remove(ur);
        await _uow.SaveChangesAsync();
    }

    public async Task AddPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        var permRepo = _uow.Repository<Domain.Entities.Security.Permission>();
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        var perm = await permRepo.GetByIdAsync(permissionId);
        if (user == null || perm == null) throw new Exception("User or Permission not found");

        var userPerm = new Domain.Entities.Security.UserPermission { UserId = userId, PermissionId = permissionId };
        await _uow.Repository<Domain.Entities.Security.UserPermission>().AddAsync(userPerm);
        await _uow.SaveChangesAsync();
    }

    public async Task RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var repo = _uow.Repository<Domain.Entities.Security.UserPermission>();
        var up = await repo.Query().FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionId == permissionId);
        if (up == null) throw new Exception("UserPermission not found");
        repo.Remove(up);
        await _uow.SaveChangesAsync();
    }

    public async Task<PagedResult<UserListData>> GetUsersPagedAsync(string? query, int page, int pageSize)
    {
        var repo = _uow.Repository<User>();
        var q = repo.Query();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim().ToLower();
            q = q.Where(u => u.Username.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(u => u.Username).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var dtos = items.Select(u => new UserListData(u.Id, u.Username, u.Email, u.IsActive, u.UserRoles.Select(ur => ur.Role.Name), u.UserPermissions.Select(up => up.Permission.Name))).ToList();
        return new PagedResult<UserListData>(dtos, total, page, pageSize);
    }
}
