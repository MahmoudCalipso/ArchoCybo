using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        // get user id from token
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var userId = Guid.Parse(userIdClaim.Value);

        var id = await _projectService.CreateProjectAsync(dto, userId);
        return CreatedAtAction(nameof(GetProject), new { id }, new { id });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetProjects()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        var userId = Guid.Parse(userIdClaim.Value);

        var projects = await _projectService.GetProjectsForUserAsync(userId);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        return Ok(project);
    }
}
