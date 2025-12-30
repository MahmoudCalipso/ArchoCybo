using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArchoCybo.Application.Features.Auth;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.SharedKernel.Security;
using Microsoft.Extensions.Configuration;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ArchoCybo.Application.Interfaces.IServices.IUserService _userService;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public AuthController(ArchoCybo.Application.Interfaces.IServices.IUserService userService, IUnitOfWork uow, IConfiguration config)
    {
        _userService = userService;
        _uow = uow;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var repo = _uow.Repository<ArchoCybo.Domain.Entities.Security.User>();
            var user = await repo.Query()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null) return Unauthorized(new { error = "Invalid credentials" });

            if (!user.IsActive) return Unauthorized(new { error = "Account is inactive" });
            if (user.IsLockedOut) return Unauthorized(new { error = "Account is locked. Try again later" });

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts += 1;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await repo.UpdateAsync(user);
                return Unauthorized(new { error = "Invalid credentials" });
            }

            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            await repo.UpdateAsync(user);

            var jwtKey = _config["Jwt:Key"] ?? "secret";
            var jwtIssuer = _config["Jwt:Issuer"] ?? "archocybo";

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Name, user.Username)
            };
            foreach (var role in user.UserRoles.Select(ur => ur.Role.Name))
            {
                claims.Add(new(System.Security.Claims.ClaimTypes.Role, role));
            }
            foreach (var permission in user.PermissionNames)
            {
                claims.Add(new("permission", permission));
            }

            var key = new System.Text.UTF8Encoding().GetBytes(jwtKey);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(10),
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature
                )
            );
            var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { token = tokenStr });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var id = await _userService.CreateUserAsync(new ArchoCybo.Application.DTOs.CreateUserDto(request.Username, request.Email, request.Password));
            return Created(string.Empty, new { id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, [FromServices] ArchoCybo.Application.Interfaces.IUnitOfWork uow)
    {
        var repo = uow.Repository<ArchoCybo.Domain.Entities.Security.User>();
        var user = await repo.Query().FirstOrDefaultAsync(u => u.Email == request.Email && u.EmailConfirmationToken == request.Token);
        if (user == null) return BadRequest(new { error = "Invalid confirmation token" });
        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await repo.UpdateAsync(user);
        return Ok(new { confirmed = true });
    }
}

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record ConfirmEmailRequest(string Email, string Token);
