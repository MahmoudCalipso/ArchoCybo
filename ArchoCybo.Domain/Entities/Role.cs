using ArchoCybo.Domain.Common;

namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Role entity for role-based access control
/// </summary>
public class Role : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;

    // Navigation Properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Computed Properties
    public int UsersCount => UserRoles.Count;
    public int PermissionsCount => RolePermissions.Count;
    public IEnumerable<string> PermissionNames => RolePermissions.Select(rp => rp.Permission.Name);
}
