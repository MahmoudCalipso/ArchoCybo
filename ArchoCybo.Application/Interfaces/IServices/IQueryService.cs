using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Interfaces.IServices;

public interface IQueryService
{
    Task<Guid> CreateCustomQueryAsync(CreateCustomQueryDto dto, Guid userId);
    Task<IEnumerable<CustomQueryDto>> GetCustomQueriesAsync(Guid projectId);
    Task UpdateCustomQueryAsync(UpdateCustomQueryDto dto);
    Task DeleteCustomQueryAsync(Guid id);
}
