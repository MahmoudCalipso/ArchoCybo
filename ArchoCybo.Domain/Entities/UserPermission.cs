using ArchoCybo.Domain.Common;

// ===================================================================
// ArchoCybo.Domain/Entities/Security/UserPermission.cs
// ===================================================================
namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Direct permission assignment to users
/// </summary>
public class UserPermission : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public Guid? GrantedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
