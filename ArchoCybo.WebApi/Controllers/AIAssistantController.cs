using ArchoCybo.Application.Services.AI;
using ArchoCybo.Domain.Entities.CodeGeneration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIAssistantController : ControllerBase
{
    private readonly OpenAIService _aiService;

    public AIAssistantController(OpenAIService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Generate entity suggestions from natural language description
    /// </summary>
    [HttpPost("suggest-entities")]
    public async Task<IActionResult> SuggestEntities([FromBody] SuggestEntitiesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Description is required");

        try
        {
            var suggestions = await _aiService.SuggestEntitiesFromDescription(request.Description);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Suggest relationships between entities
    /// </summary>
    [HttpPost("suggest-relationships")]
    public async Task<IActionResult> SuggestRelationships([FromBody] SuggestRelationshipsRequest request)
    {
        if (request.Entities == null || !request.Entities.Any())
            return BadRequest("Entities list is required");

        try
        {
            var suggestions = await _aiService.SuggestRelationships(request.Entities);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Suggest database indexes for an entity
    /// </summary>
    [HttpPost("suggest-indexes")]
    public async Task<IActionResult> SuggestIndexes([FromBody] SuggestIndexesRequest request)
    {
        if (request.Entity == null)
            return BadRequest("Entity is required");

        try
        {
            var suggestions = await _aiService.SuggestIndexes(request.Entity);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Optimize a query
    /// </summary>
    [HttpPost("optimize-query")]
    public async Task<IActionResult> OptimizeQuery([FromBody] OptimizeQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required");

        try
        {
            var optimized = await _aiService.OptimizeQuery(request.Query, request.Context ?? "");
            return Ok(new { optimizedQuery = optimized });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate README for a project
    /// </summary>
    [HttpPost("generate-readme")]
    public async Task<IActionResult> GenerateReadme([FromBody] GenerateReadmeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProjectName))
            return BadRequest("Project name is required");

        try
        {
            var readme = await _aiService.GenerateReadmeFromSchema(
                request.ProjectName, 
                request.Entities ?? new(), 
                request.DatabaseType ?? "SQL Server");
            
            return Ok(new { readme });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }

    /// <summary>
    /// Suggest validation rules for an entity
    /// </summary>
    [HttpPost("suggest-validations")]
    public async Task<IActionResult> SuggestValidations([FromBody] SuggestValidationsRequest request)
    {
        if (request.Entity == null)
            return BadRequest("Entity is required");

        try
        {
            var suggestions = await _aiService.SuggestValidationRules(request.Entity);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "AI service error", details = ex.Message });
        }
    }
}

// Request DTOs
public record SuggestEntitiesRequest(string Description);
public record SuggestRelationshipsRequest(List<Entity> Entities);
public record SuggestIndexesRequest(Entity Entity);
public record OptimizeQueryRequest(string Query, string? Context);
public record GenerateReadmeRequest(string ProjectName, List<Entity>? Entities, string? DatabaseType);
public record SuggestValidationsRequest(Entity Entity);
