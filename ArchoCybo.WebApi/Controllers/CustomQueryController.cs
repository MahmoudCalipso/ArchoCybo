using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Interfaces.IServices;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomQueryController : ControllerBase
{
    private readonly IQueryService _queryService;

    public CustomQueryController(IQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCustomQueryDto dto)
    {
        var id = await _queryService.CreateCustomQueryAsync(dto);
        return CreatedAtAction(nameof(GetByProject), new { projectId = dto.ProjectId }, new { id });
    }

    [HttpGet("project/{projectId}")]
    [Authorize]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        var list = await _queryService.GetCustomQueriesAsync(projectId);
        return Ok(list);
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateCustomQueryDto dto)
    {
        await _queryService.UpdateCustomQueryAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _queryService.DeleteCustomQueryAsync(id);
        return NoContent();
    }
}
