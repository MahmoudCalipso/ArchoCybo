using System.Text;

namespace ArchoCybo.Services;

public class CodeGenerationService
{
    public string GenerateDto(string entityName, List<ColumnInfo> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace ArchoCybo.Application.DTOs;");
        sb.AppendLine();
        sb.AppendLine($"public record {entityName}Dto(");

        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var comma = i < columns.Count - 1 ? "," : "";
            sb.AppendLine($"    {GetCSharpType(col.Type)} {col.Name}{comma}");
        }

        sb.AppendLine(");");
        sb.AppendLine();
        sb.AppendLine($"public record {entityName}FilterDto(");
        sb.AppendLine("    string? Search = null,");
        sb.AppendLine("    int Page = 1,");
        sb.AppendLine("    int PageSize = 20");
        sb.AppendLine(");");

        return sb.ToString();
    }

    public string GenerateRepository(string entityName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using ArchoCybo.Application.Interfaces;");
        sb.AppendLine($"using ArchoCybo.Domain.Entities;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace ArchoCybo.Infrastructure.Repositories;");
        sb.AppendLine();
        sb.AppendLine($"public interface I{entityName}Repository : IRepository<{entityName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    Task<IEnumerable<{entityName}>> SearchAsync(string query);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Repository : Repository<{entityName}>, I{entityName}Repository");
        sb.AppendLine("{");
        sb.AppendLine($"    public {entityName}Repository(DbContext context) : base(context) {{ }}");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<IEnumerable<{entityName}>> SearchAsync(string query)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return await Query()");
        sb.AppendLine($"            .Where(x => EF.Functions.Like(x.Name, $\"%{{query}}%\"))");
        sb.AppendLine($"            .ToListAsync();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateService(string entityName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using ArchoCybo.Application.Interfaces;");
        sb.AppendLine($"using ArchoCybo.Application.DTOs;");
        sb.AppendLine($"using ArchoCybo.Domain.Entities;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace ArchoCybo.Application.Services;");
        sb.AppendLine();
        sb.AppendLine($"public interface I{entityName}Service");
        sb.AppendLine("{");
        sb.AppendLine($"    Task<PagedResult<{entityName}Dto>> GetPagedAsync({entityName}FilterDto filter);");
        sb.AppendLine($"    Task<{entityName}Dto?> GetByIdAsync(Guid id);");
        sb.AppendLine($"    Task<Guid> CreateAsync({entityName}Dto dto);");
        sb.AppendLine($"    Task UpdateAsync(Guid id, {entityName}Dto dto);");
        sb.AppendLine($"    Task DeleteAsync(Guid id);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}Service : I{entityName}Service");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IUnitOfWork _uow;");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}Service(IUnitOfWork uow)");
        sb.AppendLine("    {");
        sb.AppendLine("        _uow = uow;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<PagedResult<{entityName}Dto>> GetPagedAsync({entityName}FilterDto filter)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var repo = _uow.Repository<{entityName}>();");
        sb.AppendLine("        var query = repo.Query();");
        sb.AppendLine();
        sb.AppendLine("        if (!string.IsNullOrWhiteSpace(filter.Search))");
        sb.AppendLine("        {");
        sb.AppendLine("            var s = filter.Search.Trim().ToLower();");
        sb.AppendLine("            query = query.Where(x => x.Name.ToLower().Contains(s));");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var total = await query.CountAsync();");
        sb.AppendLine("        var items = await query");
        sb.AppendLine("            .Skip((filter.Page - 1) * filter.PageSize)");
        sb.AppendLine("            .Take(filter.PageSize)");
        sb.AppendLine("            .ToListAsync();");
        sb.AppendLine();
        sb.AppendLine($"        var dtos = items.Select(MapToDto);");
        sb.AppendLine($"        return new PagedResult<{entityName}Dto>(dtos, total, filter.Page, filter.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<{entityName}Dto?> GetByIdAsync(Guid id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _uow.Repository<{entityName}>().GetByIdAsync(id);");
        sb.AppendLine("        return entity == null ? null : MapToDto(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<Guid> CreateAsync({entityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = MapToEntity(dto);");
        sb.AppendLine($"        await _uow.Repository<{entityName}>().AddAsync(entity);");
        sb.AppendLine("        await _uow.SaveChangesAsync();");
        sb.AppendLine("        return entity.Id;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task UpdateAsync(Guid id, {entityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _uow.Repository<{entityName}>().GetByIdAsync(id);");
        sb.AppendLine("        if (entity == null) throw new Exception(\"Not found\");");
        sb.AppendLine("        UpdateEntity(entity, dto);");
        sb.AppendLine($"        _uow.Repository<{entityName}>().Update(entity);");
        sb.AppendLine("        await _uow.SaveChangesAsync();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task DeleteAsync(Guid id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _uow.Repository<{entityName}>().GetByIdAsync(id);");
        sb.AppendLine("        if (entity == null) throw new Exception(\"Not found\");");
        sb.AppendLine($"        _uow.Repository<{entityName}>().Remove(entity);");
        sb.AppendLine("        await _uow.SaveChangesAsync();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private {entityName}Dto MapToDto({entityName} entity) => new(entity.Id, entity.Name);");
        sb.AppendLine($"    private {entityName} MapToEntity({entityName}Dto dto) => new {{ Name = dto.Name }};");
        sb.AppendLine($"    private void UpdateEntity({entityName} entity, {entityName}Dto dto) {{ entity.Name = dto.Name; }}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateController(string entityName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine($"using Microsoft.AspNetCore.Authorization;");
        sb.AppendLine($"using ArchoCybo.Application.Services;");
        sb.AppendLine($"using ArchoCybo.Application.DTOs;");
        sb.AppendLine();
        sb.AppendLine($"namespace ArchoCybo.WebApi.Controllers;");
        sb.AppendLine();
        sb.AppendLine("[ApiController]");
        sb.AppendLine("[Route(\"api/[controller]\")]");
        sb.AppendLine("[Authorize]");
        sb.AppendLine($"public class {entityName}Controller : ControllerBase");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{entityName}Service _service;");
        sb.AppendLine();
        sb.AppendLine($"    public {entityName}Controller(I{entityName}Service service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine("    public async Task<IActionResult> GetPaged([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var filter = new {entityName}FilterDto(search, page, pageSize);");
        sb.AppendLine("        var result = await _service.GetPagedAsync(filter);");
        sb.AppendLine("        return Ok(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpGet(\"{id}\")]");
        sb.AppendLine("    public async Task<IActionResult> GetById(Guid id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await _service.GetByIdAsync(id);");
        sb.AppendLine("        return result == null ? NotFound() : Ok(result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine($"    public async Task<IActionResult> Create([FromBody] {entityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        var id = await _service.CreateAsync(dto);");
        sb.AppendLine("        return CreatedAtAction(nameof(GetById), new { id }, new { id });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpPut(\"{id}\")]");
        sb.AppendLine($"    public async Task<IActionResult> Update(Guid id, [FromBody] {entityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _service.UpdateAsync(id, dto);");
        sb.AppendLine("        return NoContent();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [HttpDelete(\"{id}\")]");
        sb.AppendLine("    public async Task<IActionResult> Delete(Guid id)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _service.DeleteAsync(id);");
        sb.AppendLine("        return NoContent();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GetCSharpType(string dbType)
    {
        return dbType.ToLower() switch
        {
            "string" => "string",
            "int32" => "int",
            "int64" => "long",
            "guid" => "Guid",
            "datetime" => "DateTime",
            "boolean" => "bool",
            "decimal" => "decimal",
            "double" => "double",
            _ => "object"
        };
    }
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
