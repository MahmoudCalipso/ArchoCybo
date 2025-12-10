using ArchoCybo.Domain.Enums;

namespace ArchoCybo.Application.DTOs;

public record CreateProjectDto(string Name, string? Description, DatabaseType DatabaseType, string? DatabaseConnectionJson, bool UseBaseRoles, string? RepositoryUrl);
public record ProjectListItemDto(Guid Id, string Name, DatabaseType DatabaseType, ProjectStatus Status, DateTime CreatedAt);
public record ProjectDetailDto(Guid Id, string Name, string? Description, DatabaseType DatabaseType, string? DatabaseConnectionJson, bool UseBaseRoles, string? RepositoryUrl, ProjectStatus Status, DateTime CreatedAt, DateTime? GeneratedAt);
