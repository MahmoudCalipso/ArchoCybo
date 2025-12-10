using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Interfaces.IServices;

public interface IProjectService
{
    Task<ProjectListItemDto[]> GetProjectsForUserAsync(Guid userId);
    Task<ProjectDetailDto> GetProjectByIdAsync(Guid id);
    Task<Guid> CreateProjectAsync(CreateProjectDto dto, Guid ownerUserId);
    Task GenerateProjectAsync(Guid id, Guid triggeredByUserId);

    // New paging API
    Task<PagedResult<ProjectListItemDto>> GetProjectsPagedAsync(int page, int pageSize, string? query, string? sortBy, bool desc);
}
