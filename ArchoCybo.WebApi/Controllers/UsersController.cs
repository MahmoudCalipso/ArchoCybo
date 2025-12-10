using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces.IServices;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly INotificationPublisher _publisher;

    public UsersController(IUserService userService, INotificationPublisher publisher)
    {
        _userService = userService;
        _publisher = publisher;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
    {
        var result = await _userService.GetUsersPagedAsync(q, page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var id = await _userService.CreateUserAsync(dto);
        await _publisher.PublishUserChangedAsync(id);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(dto);
        await _publisher.PublishUserChangedAsync(dto.Id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }
}
