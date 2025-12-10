namespace ArchoCybo.Domain.Entities.CodeGeneration;

using ArchoCybo.Domain.Common;

/// <summary>
/// Minimal Project entity used by Query Builder navigation
/// </summary>
public class Project : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
}
