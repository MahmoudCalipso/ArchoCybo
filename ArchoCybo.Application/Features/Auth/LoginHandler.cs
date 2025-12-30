using MediatR;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.Extensions.Configuration;
using ArchoCybo.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Application.Features.Auth;

public class LoginHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public LoginHandler(IUnitOfWork uow, IConfiguration config)
    {
        _uow = uow;
        _config = config;
    }

    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _uow.Repository<User>();
        var user = await userRepo.Query()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user == null) throw new Exception("Invalid credentials");

        if (!user.IsActive) throw new Exception("Account is inactive");
        if (user.IsLockedOut) throw new Exception("Account is locked. Try again later");
        if (!user.EmailConfirmed) throw new Exception("Email not confirmed");

        // Verify hashed password
        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            }
            await userRepo.UpdateAsync(user);
            await _uow.SaveChangesAsync();
            throw new Exception("Invalid credentials");
        }

        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await userRepo.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        var jwtKey = _config["Jwt:Key"] ?? "secret";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "archocybo";

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username)
        };

        var roles = user.UserRoles.Select(ur => ur.Role.Name);
        foreach (var role in roles) claims.Add(new(System.Security.Claims.ClaimTypes.Role, role));

        var key = new System.Text.UTF8Encoding().GetBytes(jwtKey);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature
            )
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
