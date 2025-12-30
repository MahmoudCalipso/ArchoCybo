using Microsoft.AspNetCore.Mvc;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly ArchoCyboDbContext _db;

    public EndpointsController(ArchoCyboDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var eps = await _db.EndpointPermissions.Include(e => e.RequiredPermission).ToListAsync();
        var dtos = eps.Select(e => new EndpointAccessDto { Endpoint = e.EndpointPath, Method = e.HttpMethod, Description = e.Description ?? "", PermissionId = e.RequiredPermissionId, HasAccess = e.IsPublic }).ToList();
        return Ok(dtos);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] object model)
    {
        try
        {
            // simple binder
            var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(model.ToString() ?? "{}" );
            var ep = doc.GetProperty("Endpoint").GetString();
            var method = doc.GetProperty("Method").GetString();
            var isPublic = doc.TryGetProperty("IsPublic", out var ip) && ip.GetBoolean();
            Guid? pid = null;
            if (doc.TryGetProperty("PermissionId", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = p.GetString();
                if (Guid.TryParse(s, out var g)) pid = g;
            }

            var existing = await _db.EndpointPermissions.FirstOrDefaultAsync(x => x.EndpointPath == ep && x.HttpMethod == method);
            if (existing == null) return NotFound();
            existing.IsPublic = isPublic;
            existing.RequiredPermissionId = pid;
            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
