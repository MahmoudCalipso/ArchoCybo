using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace ArchoCybo.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenProvider _tokenProvider;
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

    public AuthStateProvider(TokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public void MarkUserAsAuthenticated(string token, string username)
    {
        _tokenProvider.Token = token;

        var claims = ParseJwtClaims(token, username);
        var identity = new ClaimsIdentity(claims, "jwt");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public void MarkUserAsLoggedOut()
    {
        _tokenProvider.Token = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    private static IEnumerable<Claim> ParseJwtClaims(string token, string fallbackUsername)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return FallbackClaims(fallbackUsername);
            var payload = parts[1];
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var claims = new List<Claim>();
            var name = root.TryGetProperty("unique_name", out var uname) ? uname.GetString()
                       : root.TryGetProperty("name", out var nm) ? nm.GetString()
                       : fallbackUsername;
            var nameId = root.TryGetProperty("nameid", out var nid) ? nid.GetString()
                        : root.TryGetProperty("sub", out var sub) ? sub.GetString()
                        : name;
            if (!string.IsNullOrEmpty(name)) claims.Add(new Claim(ClaimTypes.Name, name));
            if (!string.IsNullOrEmpty(nameId)) claims.Add(new Claim(ClaimTypes.NameIdentifier, nameId));
            if (root.TryGetProperty("role", out var roleEl))
            {
                if (roleEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in roleEl.EnumerateArray())
                    {
                        var v = r.GetString();
                        if (!string.IsNullOrEmpty(v)) claims.Add(new Claim(ClaimTypes.Role, v));
                    }
                }
                else
                {
                    var v = roleEl.GetString();
                    if (!string.IsNullOrEmpty(v)) claims.Add(new Claim(ClaimTypes.Role, v));
                }
            }
            return claims;
        }
        catch
        {
            return FallbackClaims(fallbackUsername);
        }
    }

    private static IEnumerable<Claim> FallbackClaims(string username)
    {
        return new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username)
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
