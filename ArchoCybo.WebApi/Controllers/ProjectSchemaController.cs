using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Domain.Entities.CodeGeneration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.Application.Services.Generation;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/projects/{projectId}")]
[Authorize]
public class ProjectSchemaController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationPublisher _publisher;
    private readonly IQueryService _queryService;
    private readonly ProjectGeneratorService _generator;

    public ProjectSchemaController(IUnitOfWork uow, INotificationPublisher publisher, IQueryService queryService, ProjectGeneratorService generator)
    {
        _uow = uow;
        _publisher = publisher;
        _queryService = queryService;
        _generator = generator;
    }

    [HttpGet("queries")]
    public async Task<IActionResult> GetQueries(Guid projectId)
    {
        var queries = await _queryService.GetCustomQueriesAsync(projectId);
        return Ok(queries);
    }

    [HttpGet("queries/{queryId}")]
    public async Task<IActionResult> GetQuery(Guid projectId, Guid queryId)
    {
        var queries = await _queryService.GetCustomQueriesAsync(projectId);
        var q = queries.FirstOrDefault(x => x.Id == queryId);
        if (q == null) return NotFound();
        return Ok(q);
    }

    [HttpPost("queries")]
    public async Task<IActionResult> CreateQuery(Guid projectId, [FromBody] CreateCustomQueryDto dto)
    {
        if (dto.ProjectId != projectId) return BadRequest("ProjectId mismatch");
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var id = await _queryService.CreateCustomQueryAsync(dto, userId);
        await _publisher.PublishProjectUpdatedAsync(projectId);
        return Ok(new { id });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateProject(Guid projectId)
    {
        try
        {
            var zipPath = await _generator.GenerateAsync(projectId);
            if (!System.IO.File.Exists(zipPath)) return NotFound("Generated file not found");
            
            var stream = System.IO.File.OpenRead(zipPath);
            return File(stream, "application/zip", "project-backend.zip");
        }
        catch (Exception ex)
        {
            return BadRequest($"Generation failed: {ex.Message}");
        }
    }

    [HttpGet("entities")]
    public async Task<IActionResult> GetEntities(Guid projectId)
    {
        var entities = await _uow.Repository<Entity>().Query()
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Fields)
            .Include(e => e.Relations)
            .ToListAsync();

        var dtos = entities.Select(e => new EntityDto(
            e.Id, 
            e.Name, 
            e.TableName, 
            e.Fields.Select(f => new FieldDto(f.Id, f.Name, f.DataType, f.IsNullable, f.IsPrimaryKey, f.MaxLength)).ToList(),
            e.Relations.Select(r => new RelationDto(r.Id, r.SourceEntityId, r.TargetEntityId, r.Type, r.ForeignKey, r.NavigationProperty, r.JoinTable)).ToList()
        ));

        return Ok(dtos);
    }

    [HttpPost("entities")]
    public async Task<IActionResult> CreateEntity(Guid projectId, [FromBody] CreateEntityDto dto)
    {
        var entity = new Entity
        {
            ProjectId = projectId,
            Name = dto.Name,
            TableName = dto.TableName ?? dto.Name
        };

        await _uow.Repository<Entity>().AddAsync(entity);
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return Ok(new { id = entity.Id });
    }

    [HttpDelete("entities/{entityId}")]
    public async Task<IActionResult> DeleteEntity(Guid projectId, Guid entityId)
    {
        var repo = _uow.Repository<Entity>();
        var entity = await repo.GetByIdAsync(entityId);
        if (entity == null || entity.ProjectId != projectId) return NotFound();

        repo.Remove(entity);
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return NoContent();
    }

    [HttpPost("entities/{entityId}/fields")]
    public async Task<IActionResult> CreateField(Guid projectId, Guid entityId, [FromBody] CreateFieldDto dto)
    {
        var entityRepo = _uow.Repository<Entity>();
        var entity = await entityRepo.GetByIdAsync(entityId);
        if (entity == null || entity.ProjectId != projectId) return NotFound();

        var field = new Field
        {
            Name = dto.Name,
            DataType = dto.DataType,
            IsNullable = dto.IsNullable,
            IsPrimaryKey = dto.IsPrimaryKey,
            MaxLength = dto.MaxLength,
            EntityId = entityId
        };

        var fieldRepo = _uow.Repository<Field>();
        await fieldRepo.AddAsync(field);
        
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return Ok(new { id = field.Id });
    }

    [HttpDelete("entities/{entityId}/fields/{fieldId}")]
    public async Task<IActionResult> DeleteField(Guid projectId, Guid entityId, Guid fieldId)
    {
        var fieldRepo = _uow.Repository<Field>();
        var field = await fieldRepo.GetByIdAsync(fieldId);
        if (field == null || field.EntityId != entityId) return NotFound();

        fieldRepo.Remove(field);
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return NoContent();
    }

    [HttpPost("entities/{entityId}/relations")]
    public async Task<IActionResult> CreateRelation(Guid projectId, Guid entityId, [FromBody] CreateRelationDto dto)
    {
        var entityRepo = _uow.Repository<Entity>();
        var sourceEntity = await entityRepo.GetByIdAsync(entityId);
        if (sourceEntity == null || sourceEntity.ProjectId != projectId) return NotFound();
        
        var targetEntity = await entityRepo.GetByIdAsync(dto.TargetEntityId);
        if (targetEntity == null || targetEntity.ProjectId != projectId) return BadRequest("Target entity not found in project");

        var relation = new Relation
        {
            SourceEntityId = entityId,
            TargetEntityId = dto.TargetEntityId,
            Type = dto.Type,
            ForeignKey = dto.ForeignKey,
            NavigationProperty = dto.NavigationProperty,
            JoinTable = dto.JoinTable
        };

        var relationRepo = _uow.Repository<Relation>();
        await relationRepo.AddAsync(relation);
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return Ok(new { id = relation.Id });
    }

    [HttpDelete("entities/{entityId}/relations/{relationId}")]
    public async Task<IActionResult> DeleteRelation(Guid projectId, Guid entityId, Guid relationId)
    {
        var relationRepo = _uow.Repository<Relation>();
        var relation = await relationRepo.GetByIdAsync(relationId);
        if (relation == null || relation.SourceEntityId != entityId) return NotFound();

        relationRepo.Remove(relation);
        await _uow.SaveChangesAsync();

        await _publisher.PublishProjectUpdatedAsync(projectId);

        return NoContent();
    }
}
