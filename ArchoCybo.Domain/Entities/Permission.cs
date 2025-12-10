using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Enums;
using ArchoCybo.SharedKernel.Domain;

namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Permission entity for fine-grained access control
/// </summary>
public class Permission : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public PermissionType Type { get; set; } = PermissionType.Feature;
    public bool IsSystemPermission { get; set; } = false;

    // Navigation Properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public virtual ICollection<EndpointPermission> EndpointPermissions { get; set; } = new List<EndpointPermission>();

    // Computed Properties
    public string FullName => $"{Resource}.{Action}";
    public int RolesCount => RolePermissions.Count;
    public int UsersCount => UserPermissions.Count;
}
