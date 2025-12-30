using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArchoCybo.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GitController : ControllerBase
{
    private readonly IGitService _gitService;

    public GitController(IGitService gitService)
    {
        _gitService = gitService;
    }

    [HttpGet("auth-url/{platform}")]
    public IActionResult GetAuthorizationUrl(GitPlatform platform, [FromQuery] string state)
    {
        var url = _gitService.GetAuthorizationUrl(platform, state);
        return Ok(new { url });
    }

    [HttpPost("authenticate/{platform}")]
    public async Task<IActionResult> Authenticate(GitPlatform platform, [FromBody] OAuthRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var config = await _gitService.AuthenticateAsync(userId, platform, request.Code);
        return Ok(config);
    }

    [HttpGet("organizations/{platform}")]
    public async Task<IActionResult> GetOrganizations(GitPlatform platform)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orgs = await _gitService.GetOrganizationsAsync(userId, platform);
        return Ok(orgs);
    }

    [HttpPost("create-repository/{platform}")]
    public async Task<IActionResult> CreateRepository(GitPlatform platform, [FromBody] CreateRepoRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var repoUrl = await _gitService.CreateRepositoryAsync(userId, platform, request.Name, request.Description, request.IsPrivate);
        return Ok(new { repoUrl });
    }
}

public record OAuthRequest(string Code);
public record CreateRepoRequest(string Name, string Description, bool IsPrivate);
