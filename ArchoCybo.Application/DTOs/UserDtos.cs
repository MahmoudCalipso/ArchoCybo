using ArchoCybo.Domain.Entities.Security;

namespace ArchoCybo.Application.DTOs;

public record UserListData(Guid Id, string Username, string Email, bool IsActive, IEnumerable<string> Roles, IEnumerable<string> Permissions);

public class UpdateUserDetailsDto
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserPermissionsDto
{
    public Guid UserId { get; set; }
    public List<Guid> AllowedPermissionIds { get; set; } = new();
}

public class EndpointAccessDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? PermissionId { get; set; }
    public bool HasAccess { get; set; }
}

public record UserFilterQuery(string? Search, int Page = 1, int PageSize = 20);
