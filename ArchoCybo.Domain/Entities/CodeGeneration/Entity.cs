namespace ArchoCybo.Domain.Entities.CodeGeneration;

using ArchoCybo.Domain.Common;

/// <summary>
/// Minimal Entity metadata used by Query Builder
/// </summary>
public class Entity : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ActualTableName => string.IsNullOrEmpty(TableName) ? Name : TableName;
}
