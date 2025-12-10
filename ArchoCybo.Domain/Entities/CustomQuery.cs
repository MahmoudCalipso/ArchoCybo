using ArchoCybo.Domain.Common;

namespace ArchoCybo.Domain.Entities;

public class CustomQuery : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
}
