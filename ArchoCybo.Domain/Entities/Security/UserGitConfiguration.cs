using ArchoCybo.Domain.Common;

namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Stores Git credentials and preferences for a user
/// </summary>
public class UserGitConfiguration : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public GitPlatform Platform { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
    public string? GitUsername { get; set; }
    public string? GitEmail { get; set; }
    
    // Preferences
    public string? DefaultOrganization { get; set; }
    public bool AutoCreateRepository { get; set; } = true;
    public bool IsPrivateByDefault { get; set; } = true;
}

public enum GitPlatform
{
    GitHub,
    GitLab,
    Bitbucket
}
