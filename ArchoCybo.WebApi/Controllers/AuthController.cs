using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Features.Auth;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ArchoCybo.Application.Interfaces.IServices.IUserService _userService;

    public AuthController(IMediator mediator, ArchoCybo.Application.Interfaces.IServices.IUserService userService)
    {
        _mediator = mediator;
        _userService = userService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var token = await _mediator.Send(new LoginCommand(request.Username, request.Password));
        return Ok(new { token });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var id = await _userService.CreateUserAsync(new ArchoCybo.Application.DTOs.CreateUserDto(request.Username, request.Email, request.Password));
        return Created(string.Empty, new { id });
    }
}

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password);
