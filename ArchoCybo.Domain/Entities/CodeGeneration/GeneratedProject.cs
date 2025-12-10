using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Enums;

namespace ArchoCybo.Domain.Entities.CodeGeneration;

/// <summary>
/// Represents a generated project request and metadata
/// </summary>
public class GeneratedProject : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerUserId { get; set; }

    public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;

    // JSON serialized connection settings (use DatabaseConnection VO to parse when needed)
    public string? DatabaseConnectionJson { get; set; }

    public bool UseBaseRoles { get; set; } = true;
    public string? RepositoryUrl { get; set; }
    public string? GenerationOptions { get; set; } // JSON

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    public DateTime? GeneratedAt { get; set; }
}
