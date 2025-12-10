// ===================================================================
// ArchoCybo.Domain/Entities/Security/AuditLog.cs
// ===================================================================
namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Audit log for tracking system changes
/// </summary>
public class AuditLog : ArchoCybo.SharedKernel.Domain.BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public string? Changes { get; set; } // JSON serialized changes
    public string? OldValues { get; set; } // JSON serialized old values
    public string? NewValues { get; set; } // JSON serialized new values
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Source { get; set; } // Web, API, System, etc.
}
