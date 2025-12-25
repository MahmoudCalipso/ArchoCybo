using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Domain.Entities;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Application.Services.Generation;

public class ProjectGeneratorService
{
    private readonly IUnitOfWork _uow;
    private readonly BackendCodeGeneratorService _backendGenerator;

    public ProjectGeneratorService(IUnitOfWork uow, BackendCodeGeneratorService backendGenerator)
    {
        _uow = uow;
        _backendGenerator = backendGenerator;
    }

    public async Task<string> GenerateAsync(Guid projectId)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var project = await repo.GetByIdAsync(projectId);
        if (project == null) throw new Exception("Project not found");

        // Load user for folder name
        var userRepo = _uow.Repository<ArchoCybo.Domain.Entities.Security.User>();
        var user = await userRepo.GetByIdAsync(project.OwnerUserId);
        var userName = user != null ? user.FirstName : "User";

        // Fetch Entities with Fields
        var entityRepo = _uow.Repository<Entity>();
        var entities = await entityRepo.Query()
            .Include(e => e.Fields)
            .Where(e => e.ProjectId == projectId)
            .ToListAsync();

        // Fetch Queries
        var queryRepo = _uow.Repository<CustomQuery>();
        var queries = await queryRepo.Query()
            .Where(q => q.ProjectId == projectId)
            .ToListAsync();

        // Generate Backend
        var (zipPath, projectFolder) = await _backendGenerator.GenerateBackendProject(
            project.Name, 
            project.OwnerUserId, 
            userName, 
            entities, 
            queries
        );

        // Update project status
        project.Status = ArchoCybo.Domain.Enums.ProjectStatus.Generated;
        project.GeneratedAt = DateTime.UtcNow;
        repo.Update(project);
        await _uow.SaveChangesAsync();

        return zipPath;
    }
}
