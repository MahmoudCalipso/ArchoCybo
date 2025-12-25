using ArchoCybo.Domain.Enums;

namespace ArchoCybo.Application.DTOs;

public record CreateEntityDto(string Name, string? TableName);
public record UpdateEntityDto(Guid Id, string Name, string? TableName);

public record CreateFieldDto(string Name, FieldDataType DataType, bool IsNullable, bool IsPrimaryKey, int? MaxLength);
public record UpdateFieldDto(Guid Id, string Name, FieldDataType DataType, bool IsNullable, bool IsPrimaryKey, int? MaxLength);

public record CreateRelationDto(Guid TargetEntityId, string Type, string ForeignKey, string NavigationProperty, string? JoinTable);
public record RelationDto(Guid Id, Guid SourceEntityId, Guid TargetEntityId, string Type, string ForeignKey, string NavigationProperty, string? JoinTable);

public record EntityDto(Guid Id, string Name, string TableName, List<FieldDto> Fields, List<RelationDto> Relations);
public record FieldDto(Guid Id, string Name, FieldDataType DataType, bool IsNullable, bool IsPrimaryKey, int? MaxLength);
