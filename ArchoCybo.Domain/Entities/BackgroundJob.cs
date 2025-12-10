using ArchoCybo.Domain.Common;

namespace ArchoCybo.Domain.Entities;

public enum BackgroundJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public class BackgroundJob : BaseAuditableEntity
{
    public Guid ProjectId { get; set; }
    public Guid TriggeredByUserId { get; set; }
    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Pending;
    public int Attempts { get; set; } = 0;
    public string? LastError { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
