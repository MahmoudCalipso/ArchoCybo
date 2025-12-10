using ArchoCybo.Domain.Common;

// ===================================================================
// ArchoCybo.Domain/Entities/Security/UserSession.cs
// ===================================================================
namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// User session tracking for multi-device support
/// </summary>
public class UserSession : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevokedReason { get; set; }

    // Computed Properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsValid => IsActive && !IsExpired && !IsRevoked;
}
