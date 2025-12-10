using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.Infrastructure.Data;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetadataController : ControllerBase
{
    private readonly ArchoCyboDbContext _db;

    public MetadataController(ArchoCyboDbContext db)
    {
        _db = db;
    }

    [HttpGet("entities")]
    public IActionResult GetEntities()
    {
        var model = _db.Model;
        var entities = model.GetEntityTypes()
            .Select(e => new {
                Name = e.ClrType.Name,
                Schema = e.GetSchema(),
                TableName = e.GetTableName(),
                Properties = e.GetProperties().Select(p => new { Name = p.Name, Type = p.ClrType.Name })
            })
            .ToList();

        return Ok(entities);
    }

    [HttpGet("entity/{name}/columns")]
    public IActionResult GetEntityColumns(string name)
    {
        var et = _db.Model.GetEntityTypes().FirstOrDefault(e => e.ClrType.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (et == null) return NotFound();

        var cols = et.GetProperties().Select(p => new { Name = p.Name, Type = p.ClrType.Name }).ToList();
        return Ok(cols);
    }
}
