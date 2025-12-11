namespace ArchoCybo.Domain.Entities.CodeGeneration;

using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Entities.Security;

/// <summary>
/// Minimal Project entity used by Query Builder navigation
/// </summary>
public class Project : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
    // FK to User
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Optional: collection of entities within project
    public virtual ICollection<Entity> Entities { get; set; } = new List<Entity>();
}
