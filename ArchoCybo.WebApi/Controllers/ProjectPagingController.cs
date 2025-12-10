using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/project")]
public class ProjectPagingController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectPagingController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? q = null, [FromQuery] string? sortBy = null, [FromQuery] bool desc = false)
    {
        var result = await _projectService.GetProjectsPagedAsync(page, pageSize, q, sortBy, desc);
        return Ok(result);
    }
}
