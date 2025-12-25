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
    private readonly INotificationPublisher _publisher;

    public CustomQueryController(IQueryService queryService, INotificationPublisher publisher)
    {
        _queryService = queryService;
        _publisher = publisher;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCustomQueryDto dto)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        var id = await _queryService.CreateCustomQueryAsync(dto, userId);
        await _publisher.PublishProjectUpdatedAsync(dto.ProjectId);
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
