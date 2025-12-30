using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities.CodeGeneration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ArchoCybo.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _uow;

    public ProjectService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> CreateProjectAsync(CreateProjectDto dto, Guid ownerUserId)
    {
        var project = new GeneratedProject
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerUserId = ownerUserId,
            DatabaseType = dto.DatabaseType,
            DatabaseConnectionJson = dto.DatabaseConnectionJson,
            UseBaseRoles = dto.UseBaseRoles,
            RepositoryUrl = dto.RepositoryUrl,
            Status = ArchoCybo.Domain.Enums.ProjectStatus.Draft
        };

        var result = await _uow.Repository<GeneratedProject>().AddAsync(project);
        if (!result.Success) throw new Exception(result.Message);
        return project.Id;
    }

    public async Task<ProjectListItemDto[]> GetProjectsForUserAsync(Guid userId)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var projects = await repo.Query().Where(p => p.OwnerUserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync();
        return projects.Select(p => new ProjectListItemDto(p.Id, p.Name, p.DatabaseType, p.Status, p.CreatedAt)).ToArray();
    }

    public async Task<ProjectDetailDto> GetProjectByIdAsync(Guid id)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var result = await repo.GetByIdAsync(id);
        if (!result.Success || result.Data == null) throw new Exception("Project not found");
        var p = result.Data;
        return new ProjectDetailDto(p.Id, p.Name, p.Description, p.DatabaseType, p.DatabaseConnectionJson, p.UseBaseRoles, p.RepositoryUrl, p.Status, p.CreatedAt, p.GeneratedAt);
    }

    public async Task GenerateProjectAsync(Guid id, Guid triggeredByUserId)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var result = await repo.GetByIdAsync(id);
        if (!result.Success || result.Data == null) throw new Exception("Project not found");
        var project = result.Data;

        // mark as in progress
        project.Status = ArchoCybo.Domain.Enums.ProjectStatus.InProgress;
        await repo.UpdateAsync(project);

        // Simulate generation work (in real system you'd enqueue a background job)
        await Task.Delay(1000);

        // If UseBaseRoles is true, copy base roles/permissions snapshot into generation metadata (simplified)
        if (project.UseBaseRoles)
        {
            var roleRepo = _uow.Repository<ArchoCybo.Domain.Entities.Security.Role>();
            var rolesResult = await roleRepo.GetAllAsync();
            if (rolesResult.Success && rolesResult.Data != null)
            {
                var roleSnapshot = JsonSerializer.Serialize(rolesResult.Data);
                project.GenerationOptions = roleSnapshot;
            }
        }

        // persist generated files placeholder
        project.Status = ArchoCybo.Domain.Enums.ProjectStatus.Generated;
        project.GeneratedAt = DateTime.UtcNow;
        await repo.UpdateAsync(project);
    }

    public async Task<PagedResult<ProjectListItemDto>> GetProjectsPagedAsync(int page, int pageSize, string? query, string? sortBy, bool desc)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var q = repo.Query();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        var total = await q.CountAsync();

        // sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (sortBy.Equals("name", StringComparison.OrdinalIgnoreCase)) q = desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name);
            else if (sortBy.Equals("createdat", StringComparison.OrdinalIgnoreCase)) q = desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt);
            else q = q.OrderByDescending(p => p.CreatedAt);
        }
        else
        {
            q = q.OrderByDescending(p => p.CreatedAt);
        }

        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var dtos = items.Select(p => new ProjectListItemDto(p.Id, p.Name, p.DatabaseType, p.Status, p.CreatedAt));
        return new PagedResult<ProjectListItemDto>(dtos, total, page, pageSize);
    }
}
