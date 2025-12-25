using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.DTOs;

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

    [HttpPut("{id}/details")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateUserDetailsDto dto)
    {
        await _userService.UpdateUserDetailsAsync(id, dto);
        await _publisher.PublishUserChangedAsync(id);
        return Ok();
    }

    [HttpPut("{id}/roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] List<Guid> roleIds)
    {
        await _userService.UpdateUserRolesAsync(id, roleIds);
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateUserPermissionsDto dto)
    {
        dto.UserId = id;
        await _userService.UpdateUserPermissionsAsync(id, dto);
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }
    [HttpGet("{id}/endpoints")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetEndpointAccess(Guid id)
    {
        var result = await _userService.GetUserEndpointAccessAsync(id);
        return Ok(result);
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
