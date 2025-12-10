using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ArchoCybo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly ArchoCyboDbContext _db;

    public QueryController(ArchoCyboDbContext db)
    {
        _db = db;
    }

    [HttpPost("execute")]
    [Authorize]
    public async Task<IActionResult> Execute([FromBody] QueryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Sql)) return BadRequest("SQL is empty");

        var sql = dto.Sql.Trim();
        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only SELECT statements are allowed");

        using var conn = (SqlConnection)_db.Database.GetDbConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = dto.TimeoutSeconds ?? 30;

        if (dto.Parameters != null)
        {
            foreach (var p in dto.Parameters)
            {
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }
        }

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var val = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                row[name] = val;
            }
            list.Add(row);
        }
        await conn.CloseAsync();
        return Ok(list);
    }
}
