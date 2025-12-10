using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Interfaces.IServices;

public interface IUserService
{
    Task<List<UserListData>> GetUsersAsync(UserFilterQuery filter);
    Task<PagedResult<UserListData>> GetUsersPagedAsync(string? query, int page, int pageSize);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, Guid roleId);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task AddPermissionToUserAsync(Guid userId, Guid permissionId);
    Task RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    // CRUD
    Task<Guid> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(UpdateUserDto dto);
    Task DeleteUserAsync(Guid userId);
}
