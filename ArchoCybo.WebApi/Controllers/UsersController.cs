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

    /// <summary>Gets a paged list of users</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<UserListData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null)
    {
        var result = await _userService.GetUsersPagedAsync(q, page, pageSize);
        return Ok(result);
    }

    /// <summary>Creates a new user</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var id = await _userService.CreateUserAsync(dto);
        await _publisher.PublishUserChangedAsync(id);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    /// <summary>Updates basic user fields</summary>
    [HttpPut]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(dto);
        await _publisher.PublishUserChangedAsync(dto.Id);
        return NoContent();
    }

    /// <summary>Updates detailed personal information of a user</summary>
    [HttpPut("{id}/details")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateUserDetailsDto dto)
    {
        var actingUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        try
        {
            await _userService.UpdateUserDetailsAsync(actingUserId, id, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        await _publisher.PublishUserChangedAsync(id);
        return Ok();
    }

    /// <summary>Allows the current authenticated user to update their own personal information</summary>
    [HttpPut("me/details")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyDetails([FromBody] UpdateUserDetailsDto dto)
    {
        var actingUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _userService.UpdateUserDetailsAsync(actingUserId, actingUserId, dto);
        await _publisher.PublishUserChangedAsync(actingUserId);
        return Ok();
    }

    /// <summary>Updates roles assigned to a user</summary>
    [HttpPut("{id}/roles")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] List<Guid> roleIds)
    {
        if (roleIds == null)
        {
            return BadRequest("roleIds cannot be null");
        }
        var actingUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        try
        {
            await _userService.UpdateUserRolesAsync(actingUserId, id, roleIds);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }

    /// <summary>Updates direct permissions assigned to a user</summary>
    [HttpPut("{id}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateUserPermissionsDto dto)
    {
        dto.UserId = id;
        var actingUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        try
        {
            await _userService.UpdateUserPermissionsAsync(actingUserId, id, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }
    /// <summary>Gets the list of API endpoints and whether the user has access</summary>
    [HttpGet("{id}/endpoints")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(List<EndpointAccessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEndpointAccess(Guid id)
    {
        var result = await _userService.GetUserEndpointAccessAsync(id);
        return Ok(result);
    }
    
    /// <summary>Gets all roles</summary>
    [HttpGet("roles")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(List<RoleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _userService.GetAllRolesAsync();
        return Ok(roles);
    }
    
    /// <summary>Gets all permissions</summary>
    [HttpGet("permissions")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(List<PermissionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var perms = await _userService.GetAllPermissionsAsync();
        return Ok(perms);
    }

    [HttpGet("roles/{id}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetRolePermissions(Guid id)
    {
        var result = await _userService.GetRolePermissionsAsync(id);
        return Ok(result);
    }

    [HttpPut("roles/{id}/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] List<Guid> permissionIds)
    {
        var actingUserId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _userService.UpdateRolePermissionsAsync(actingUserId, id, permissionIds);
        return NoContent();
    }

    [HttpGet("endpoints/all")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllEndpoints()
    {
        var result = await _userService.GetAllEndpointsAsync();
        return Ok(result);
    }

    /// <summary>Deletes a user</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        await _publisher.PublishUserChangedAsync(id);
        return NoContent();
    }
}
