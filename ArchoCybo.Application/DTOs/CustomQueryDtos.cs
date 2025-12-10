namespace ArchoCybo.Application.DTOs;

public record CustomQueryDto(Guid Id, string Name, string Sql, Guid ProjectId, DateTime CreatedAt);
public record CreateCustomQueryDto(string Name, string Sql, Guid ProjectId);
public record UpdateCustomQueryDto(Guid Id, string Name, string Sql);
