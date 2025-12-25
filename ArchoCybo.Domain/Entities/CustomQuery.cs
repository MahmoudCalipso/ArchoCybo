using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Entities.Security;

namespace ArchoCybo.Domain.Entities;

public class CustomQuery : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string? ResultSchema { get; set; }

    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }
}
