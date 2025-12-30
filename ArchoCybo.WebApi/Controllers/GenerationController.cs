using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Interfaces.IServices.Background;
using ArchoCybo.Application.Services.Generation;
using ArchoCybo.Application.Interfaces.IServices;
using Hangfire;
using ArchoCybo.WebApi.Services;
using System.IO;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenerationController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IBackgroundJobQueue _queue;
    private readonly ProjectGeneratorService _generator;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public GenerationController(IProjectService projectService, IBackgroundJobQueue queue, ProjectGeneratorService generator, IBackgroundJobClient backgroundJobClient)
    {
        _projectService = projectService;
        _queue = queue;
        _generator = generator;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpPost("{id}/generate")]
    [Authorize]
    public async Task<IActionResult> GenerateProject(Guid id)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var userId = Guid.Parse(userIdClaim.Value);

        // enqueue hangfire job
        _backgroundJobClient.Enqueue<HangfireJobService>(h => h.RunGeneration(id, userId));
        return Accepted();
    }

    [HttpPost("{id}/generate-sync")]
    [Authorize]
    public async Task<IActionResult> GenerateProjectSync(Guid id)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var userId = Guid.Parse(userIdClaim.Value);

        // synchronous generate and return zip path
        var zip = await _generator.GenerateAsync(id);
        return Ok(new { zip });
    }

    [HttpGet("{id}/artifact")]
    [Authorize]
    public IActionResult GetArtifact(Guid id)
    {
        // find project by id and return the zip stored in GenerationOptions
        var repo = HttpContext.RequestServices.GetRequiredService<ArchoCybo.Application.Interfaces.IUnitOfWork>().Repository<ArchoCybo.Domain.Entities.CodeGeneration.GeneratedProject>();
        var result = repo.GetByIdAsync(id).GetAwaiter().GetResult();
        if (!result.Success || result.Data == null) return NotFound();
        var project = result.Data;

        if (string.IsNullOrEmpty(project.GenerationOptions)) return NotFound();
        try
        {
            var meta = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(project.GenerationOptions);
            if (meta.TryGetProperty("ArtifactZip", out var az))
            {
                var path = az.GetString();
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    var stream = System.IO.File.OpenRead(path);
                    return File(stream, "application/zip", Path.GetFileName(path));
                }
            }
        }
        catch
        {
            return NotFound();
        }

        return NotFound();
    }
}
