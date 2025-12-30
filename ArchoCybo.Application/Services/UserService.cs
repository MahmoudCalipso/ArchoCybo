using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.SharedKernel.Security;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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
        // Basic field validation
        if (string.IsNullOrWhiteSpace(dto.Username)) throw new Exception("Username is required");
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new Exception("Email is required");
        if (string.IsNullOrWhiteSpace(dto.Password)) throw new Exception("Password is required");

        var emailValidator = new EmailAddressAttribute();
        if (!emailValidator.IsValid(dto.Email)) throw new Exception("Invalid email format");

        // Enforce password strength: min 8, upper, lower, digit, special
        if (dto.Password.Length < 8 ||
            !Regex.IsMatch(dto.Password, "[A-Z]") ||
            !Regex.IsMatch(dto.Password, "[a-z]") ||
            !Regex.IsMatch(dto.Password, "[0-9]") ||
            !Regex.IsMatch(dto.Password, "[^A-Za-z0-9]"))
        {
            throw new Exception("Password must be at least 8 characters and include upper, lower, digit, and special character");
        }

        // Uniqueness checks
        var usernameExists = await _uow.Repository<User>().Query().AnyAsync(u => u.Username == dto.Username);
        if (usernameExists) throw new Exception("Username is already taken");
        var emailExists = await _uow.Repository<User>().Query().AnyAsync(u => u.Email == dto.Email);
        if (emailExists) throw new Exception("Email is already in use");

        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = PasswordHasher.Hash(dto.Password),
            IsActive = true,
            EmailConfirmed = false,
            EmailConfirmationToken = Guid.NewGuid().ToString("N")
        };
        var result = await _uow.Repository<User>().AddAsync(user);
        if (!result.Success) throw new Exception(result.Message);

        // Assign default 'User' role
        var roleRepo = _uow.Repository<Role>();
        var defaultRole = await roleRepo.Query().FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole != null)
        {
            var userRole = new UserRole { UserId = user.Id, RoleId = defaultRole.Id };
            await _uow.Repository<UserRole>().AddAsync(userRole);
        }
        return user.Id;
    }

    public async Task UpdateUserAsync(UpdateUserDto dto)
    {
        var repo = _uow.Repository<User>();
        var result = await repo.GetByIdAsync(dto.Id);
        if (!result.Success || result.Data == null) throw new Exception("User not found");
        var user = result.Data;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        await repo.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var repo = _uow.Repository<User>();
        var result = await repo.DeleteAsync(userId);
        if (!result.Success) throw new Exception("User not found");
    }

    public async Task UpdateUserDetailsAsync(Guid actingUserId, Guid userId, UpdateUserDetailsDto dto)
    {
        var repo = _uow.Repository<User>();
        var result = await repo.GetByIdAsync(userId);
        if (!result.Success || result.Data == null) throw new Exception("User not found");
        var user = result.Data;

        // Uniqueness checks
        var emailExists = await _uow.Repository<User>().Query().AnyAsync(u => u.Email == dto.Email && u.Id != userId);
        if (emailExists) throw new Exception("Email is already in use");
        var usernameExists = await _uow.Repository<User>().Query().AnyAsync(u => u.Username == dto.Username && u.Id != userId);
        if (usernameExists) throw new Exception("Username is already taken");

        var oldValues = new
        {
            user.Email,
            user.Username,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.Avatar,
            user.IsActive
        };

        user.Email = dto.Email.Trim();
        user.Username = dto.Username.Trim();
        if (!string.IsNullOrEmpty(dto.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash(dto.PasswordHash);
        }
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.FirstName = dto.FirstName?.Trim();
        user.LastName = dto.LastName?.Trim();
        user.Avatar = dto.Avatar?.Trim();
        user.IsActive = dto.IsActive;

        await repo.UpdateAsync(user);

        var newValues = new
        {
            user.Email,
            user.Username,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.Avatar,
            user.IsActive
        };

        var audit = new Domain.Entities.Security.AuditLog
        {
            UserId = actingUserId,
            EntityName = nameof(User),
            EntityId = user.Id.ToString(),
            Action = "Updated",
            OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
            Changes = null,
            Timestamp = DateTime.UtcNow,
            Source = "API"
        };
        await _uow.Repository<Domain.Entities.Security.AuditLog>().AddAsync(audit);
    }

    public async Task UpdateUserPermissionsAsync(Guid actingUserId, Guid userId, UpdateUserPermissionsDto dto)
    {
        // Prevent permission escalation: only allow changing permissions if acting user has equal or higher priority than target user's highest role
        var rolesRepo = _uow.Repository<Role>();
        var targetResult = await _uow.Repository<User>().GetByIdAsync(userId);
        var actingResult = await _uow.Repository<User>().GetByIdAsync(actingUserId);
        
        if (!targetResult.Success || !actingResult.Success) throw new Exception("User not found");
        var targetUser = targetResult.Data!;
        var actingUser = actingResult.Data!;

        // We need roles for priority check - using Query for that
        var targetUserWithRoles = await _uow.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var actingUserWithRoles = await _uow.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == actingUserId);
            
        if (targetUserWithRoles == null || actingUserWithRoles == null) throw new Exception("User not found");
        var targetMaxPriority = targetUserWithRoles.UserRoles.Select(ur => ur.Role.Priority).DefaultIfEmpty(0).Max();
        var actingMaxPriority = actingUserWithRoles.UserRoles.Select(ur => ur.Role.Priority).DefaultIfEmpty(0).Max();
        if (actingMaxPriority < targetMaxPriority)
        {
            throw new Exception("Cannot modify permissions of a user with higher role priority");
        }

        var repo = _uow.Repository<Domain.Entities.Security.UserPermission>();
        var existing = await repo.Query().Where(x => x.UserId == userId).ToListAsync();
        
        foreach (var item in existing)
        {
            await repo.DeleteAsync(item.Id);
        }

        foreach (var pid in dto.AllowedPermissionIds)
        {
            await repo.AddAsync(new Domain.Entities.Security.UserPermission { UserId = userId, PermissionId = pid });
        }
        await _uow.SaveChangesAsync();

        var audit = new Domain.Entities.Security.AuditLog
        {
            UserId = actingUserId,
            EntityName = nameof(User),
            EntityId = userId.ToString(),
            Action = "PermissionsUpdated",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { dto.AllowedPermissionIds }),
            Timestamp = DateTime.UtcNow,
            Source = "API"
        };
        await _uow.Repository<Domain.Entities.Security.AuditLog>().AddAsync(audit);
    }

    public async Task<List<EndpointAccessDto>> GetUserEndpointAccessAsync(Guid userId)
    {
        var endpoints = await _uow.Repository<EndpointPermission>().Query().Include(e => e.RequiredPermission).ToListAsync();
        
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.UserPermissions)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new Exception("User not found");

        var userPermissionIds = user.UserPermissions.Select(up => up.PermissionId)
            .Union(user.UserRoles.SelectMany(ur => ur.Role.RolePermissions).Select(rp => rp.PermissionId))
            .ToHashSet();

        return endpoints.Select(e => new EndpointAccessDto
        {
            Endpoint = e.EndpointPath,
            Method = e.HttpMethod,
            Description = e.Description ?? "",
            PermissionId = e.RequiredPermissionId,
            HasAccess = e.IsPublic || (e.RequiredPermissionId.HasValue && userPermissionIds.Contains(e.RequiredPermissionId.Value))
        }).ToList();
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

    public async Task UpdateUserRolesAsync(Guid actingUserId, Guid userId, List<Guid> roleIds)
    {
        var roleRepo = _uow.Repository<Role>();
        var targetUser = await _uow.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var actingUser = await _uow.Repository<User>().Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == actingUserId);
        if (targetUser == null || actingUser == null) throw new Exception("User not found");
        var targetMaxPriority = targetUser.UserRoles.Select(ur => ur.Role.Priority).DefaultIfEmpty(0).Max();
        var actingMaxPriority = actingUser.UserRoles.Select(ur => ur.Role.Priority).DefaultIfEmpty(0).Max();
        if (actingMaxPriority < targetMaxPriority)
        {
            throw new Exception("Cannot modify roles of a user with higher role priority");
        }
        // Also prevent assigning roles with higher priority than acting user's highest
        var rolesToAssign = await roleRepo.Query().Where(r => roleIds.Contains(r.Id)).ToListAsync();
        if (rolesToAssign.Any(r => r.Priority > actingMaxPriority || r.IsSystemRole && actingUser.UserRoles.All(ur => ur.Role.Name != "SuperUser")))
        {
            throw new Exception("Attempt to assign roles beyond allowed priority or system roles");
        }

        var repo = _uow.Repository<UserRole>();
        var existing = await repo.Query().Where(x => x.UserId == userId).ToListAsync();
        foreach (var ur in existing) await repo.DeleteAsync(ur.Id);
        foreach (var rid in roleIds) await repo.AddAsync(new UserRole { UserId = userId, RoleId = rid });

        var audit = new Domain.Entities.Security.AuditLog
        {
            UserId = actingUserId,
            EntityName = nameof(User),
            EntityId = userId.ToString(),
            Action = "RolesUpdated",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { roleIds }),
            Timestamp = DateTime.UtcNow,
            Source = "API"
        };
        await _uow.Repository<Domain.Entities.Security.AuditLog>().AddAsync(audit);
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
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var repo = _uow.Repository<Domain.Entities.Security.UserRole>();
        var ur = await repo.Query().FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId);
        if (ur == null) throw new Exception("UserRole not found");
        await repo.DeleteAsync(ur.Id);
    }

    public async Task AddPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        var permRepo = _uow.Repository<Domain.Entities.Security.Permission>();
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        var perm = await permRepo.GetByIdAsync(permissionId);
        if (user == null || perm == null) throw new Exception("User or Permission not found");

        var userPerm = new Domain.Entities.Security.UserPermission { UserId = userId, PermissionId = permissionId };
        await _uow.Repository<Domain.Entities.Security.UserPermission>().AddAsync(userPerm);
    }

    public async Task RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var repo = _uow.Repository<Domain.Entities.Security.UserPermission>();
        var up = await repo.Query().FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionId == permissionId);
        if (up == null) throw new Exception("UserPermission not found");
        await repo.DeleteAsync(up.Id);
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

    public async Task<List<RoleSummaryDto>> GetAllRolesAsync()
    {
        var roles = await _uow.Repository<Role>().Query().ToListAsync();
        return roles.Select(r => new RoleSummaryDto(r.Id, r.Name, r.DisplayName, r.Priority, r.IsSystemRole)).ToList();
    }

    public async Task<List<PermissionSummaryDto>> GetAllPermissionsAsync()
    {
        var perms = await _uow.Repository<Permission>().Query().ToListAsync();
        return perms.Select(p => new PermissionSummaryDto(p.Id, p.Name, p.DisplayName, p.Category, p.Resource, p.Action, p.IsSystemPermission)).ToList();
    }

    public async Task<List<PermissionSummaryDto>> GetRolePermissionsAsync(Guid roleId)
    {
        var role = await _uow.Repository<Role>().Query()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId);
        
        if (role == null) throw new Exception("Role not found");
        
        return role.RolePermissions.Select(rp => new PermissionSummaryDto(rp.Permission.Id, rp.Permission.Name, rp.Permission.DisplayName, rp.Permission.Category, rp.Permission.Resource, rp.Permission.Action, rp.Permission.IsSystemPermission)).ToList();
    }

    public async Task UpdateRolePermissionsAsync(Guid actingUserId, Guid roleId, List<Guid> permissionIds)
    {
        var repo = _uow.Repository<Domain.Entities.Security.RolePermission>();
        var existing = await repo.Query().Where(x => x.RoleId == roleId).ToListAsync();
        
        foreach (var item in existing) await repo.DeleteAsync(item.Id);
        foreach (var pid in permissionIds)
        {
            await repo.AddAsync(new Domain.Entities.Security.RolePermission { RoleId = roleId, PermissionId = pid });
        }

        // Audit
        var audit = new Domain.Entities.Security.AuditLog
        {
            UserId = actingUserId,
            EntityName = nameof(Role),
            EntityId = roleId.ToString(),
            Action = "PermissionsUpdated",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { permissionIds }),
            Timestamp = DateTime.UtcNow,
            Source = "API"
        };
        await _uow.Repository<Domain.Entities.Security.AuditLog>().AddAsync(audit);
    }

    public async Task<List<EndpointAccessDto>> GetAllEndpointsAsync()
    {
        var endpoints = await _uow.Repository<EndpointPermission>().Query().Include(e => e.RequiredPermission).ToListAsync();
        return endpoints.Select(e => new EndpointAccessDto
        {
            Endpoint = e.EndpointPath,
            Method = e.HttpMethod,
            Description = e.Description ?? "",
            PermissionId = e.RequiredPermissionId,
            HasAccess = false // Not user specific
        }).ToList();
    }
}
