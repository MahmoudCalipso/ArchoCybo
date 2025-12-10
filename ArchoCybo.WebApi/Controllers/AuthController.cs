using Microsoft.AspNetCore.Mvc;
using MediatR;
using ArchoCybo.Application.Features.Auth;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var token = await _mediator.Send(new LoginCommand(request.Username, request.Password));
        return Ok(new { token });
    }
}

public record LoginRequest(string Username, string Password);
