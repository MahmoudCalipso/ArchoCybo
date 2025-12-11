namespace ArchoCybo.Domain.Common;

public abstract class BaseAuditableEntity : ArchoCybo.SharedKernel.Domain.BaseEntity
{
    public Guid CreatedBy { get; set; } 
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Soft-delete and audit support
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
