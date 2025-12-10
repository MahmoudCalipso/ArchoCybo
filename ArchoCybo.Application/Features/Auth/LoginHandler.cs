using MediatR;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.Extensions.Configuration;
using ArchoCybo.SharedKernel.Security;

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
        var users = await userRepo.ListAsync();
        var user = users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null) throw new Exception("Invalid credentials");

        // Verify hashed password
        if (!PasswordHasher.Verify(request.Password, user.PasswordHash)) throw new Exception("Invalid credentials");

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
