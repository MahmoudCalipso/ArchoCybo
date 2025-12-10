namespace ArchoCybo.Application.DTOs;

public record QueryDto(string Sql, Dictionary<string, object?>? Parameters = null, int? TimeoutSeconds = null);
