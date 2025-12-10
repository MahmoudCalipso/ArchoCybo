using ArchoCybo.Domain.Common;

namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Many-to-many relationship between Roles and Permissions
/// </summary>
public class RolePermission : BaseAuditableEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
