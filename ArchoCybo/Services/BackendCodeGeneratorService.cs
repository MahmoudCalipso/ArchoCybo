using System.Text;
using System.IO.Compression;

namespace ArchoCybo.Services;

public class BackendCodeGeneratorService
{
    public async Task<(string zipPath, string projectFolder)> GenerateBackendProject(
        string projectName,
        string userId,
        List<EntityDefinition> entities,
        List<QueryDefinition> queries)
    {
        var baseFolder = Path.Combine(Path.GetTempPath(), "PROJECT-GEN-AI", $"{userId}-{projectName}");
        var projectFolder = Path.Combine(baseFolder, projectName);
        var backendFolder = Path.Combine(projectFolder, "Backend");

        Directory.CreateDirectory(backendFolder);

        await GenerateProjectStructure(backendFolder, projectName, entities, queries);

        var zipPath = Path.Combine(baseFolder, $"{projectName}-Backend.zip");
        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(backendFolder, zipPath);

        return (zipPath, projectFolder);
    }

    private async Task GenerateProjectStructure(string basePath, string projectName, List<EntityDefinition> entities, List<QueryDefinition> queries)
    {
        var dirs = new[]
        {
            "Domain/Entities",
            "Domain/Enums",
            "Application/DTOs",
            "Application/Interfaces",
            "Application/Services",
            "Infrastructure/Data",
            "Infrastructure/Repositories",
            "WebApi/Controllers",
            "WebApi/Middleware",
            "SharedKernel"
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(basePath, dir));
        }

        await GenerateCsprojFile(basePath, projectName);
        await GenerateProgramCs(basePath, projectName);
        await GenerateEntities(basePath, entities);
        await GenerateDTOs(basePath, entities, queries);
        await GenerateRepositories(basePath, entities);
        await GenerateServices(basePath, entities);
        await GenerateControllers(basePath, entities, queries);
        await GenerateDbContext(basePath, projectName, entities);
        await GenerateGlobalExceptionMiddleware(basePath, projectName);
    }

    private async Task GenerateGlobalExceptionMiddleware(string basePath, string projectName)
    {
        var content = $@"using System.Net;
using System.Text.Json;

namespace {projectName}.WebApi.Middleware;

public class GlobalExceptionMiddleware
{{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {{
        _next = next;
    }}

    public async Task InvokeAsync(HttpContext context)
    {{
        try
        {{
            await _next(context);
        }}
        catch (Exception ex)
        {{
            await HandleExceptionAsync(context, ex);
        }}
    }}

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {{
        context.Response.ContentType = ""application/json"";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {{
            StatusCode = context.Response.StatusCode,
            Message = ""Internal Server Error from generated code"",
            Detailed = exception.Message
        }};

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }}
}}";
        await File.WriteAllTextAsync(Path.Combine(basePath, "WebApi", "Middleware", "GlobalExceptionMiddleware.cs"), content);
    }

    private async Task GenerateCsprojFile(string basePath, string projectName)
    {
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""10.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""10.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""10.0.0"" />
    <PackageReference Include=""MediatR"" Version=""12.0.0"" />
    <PackageReference Include=""MediatR.Extensions.Microsoft.DependencyInjection"" Version=""12.0.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.JwtBearer"" Version=""10.0.0"" />
  </ItemGroup>

</Project>";

        await File.WriteAllTextAsync(Path.Combine(basePath, $"{projectName}.csproj"), content);
    }

    private async Task GenerateProgramCs(string basePath, string projectName)
    {
        var content = $@"using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using {projectName}.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddMediatR(typeof(Program));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {{
        var key = Encoding.ASCII.GetBytes(builder.Configuration[""JwtSettings:SecretKey""] ?? """");
        options.TokenValidationParameters = new TokenValidationParameters
        {{
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
        }};
    }});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{{
    options.AddPolicy(""AllowAll"", builder =>
    {{
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    }});
}});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{{
    app.UseDeveloperExceptionPage();
}}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseCors(""AllowAll"");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Program.cs"), content);
    }

    private async Task GenerateEntities(string basePath, List<EntityDefinition> entities)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace {entity.ProjectName}.Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"public class {entity.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    public Guid Id {{ get; set; }} = Guid.NewGuid();");

            foreach (var prop in entity.Properties)
            {
                var annotations = string.Empty;
                if (prop.Annotations.Any())
                {
                    annotations = string.Join("\n    ", prop.Annotations.Select(a => $"[{a}]"));
                    annotations = "\n    " + annotations + "\n    ";
                }

                var type = prop.IsNullable ? $"{prop.DataType}?" : prop.DataType;
                sb.AppendLine($"{annotations}public {type} {prop.Name} {{ get; set; }}{(prop.IsNullable ? " = null;" : "")}");
            }

            sb.AppendLine($"    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;");
            sb.AppendLine($"    public DateTime? UpdatedAt {{ get; set; }}");
            sb.AppendLine("}");

            var filePath = Path.Combine(basePath, "Domain", "Entities", $"{entity.Name}.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateDTOs(string basePath, List<EntityDefinition> entities, List<QueryDefinition> queries)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {entity.ProjectName}.Application.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"public record {entity.Name}Dto(");
            sb.AppendLine("    Guid Id,");

            var props = entity.Properties.ToList();
            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var type = props[i].IsNullable ? $"{props[i].DataType}?" : props[i].DataType;
                sb.AppendLine($"    {type} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine($"public record Create{entity.Name}Dto(");

            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var type = props[i].IsNullable ? $"{props[i].DataType}?" : props[i].DataType;
                sb.AppendLine($"    {type} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine($"public record Update{entity.Name}Dto(");
            sb.AppendLine("    Guid Id,");

            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var type = props[i].IsNullable ? $"{props[i].DataType}?" : props[i].DataType;
                sb.AppendLine($"    {type} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine($"public record {entity.Name}FilterDto(");
            sb.AppendLine("    string? Search = null,");
            sb.AppendLine("    int Page = 1,");
            sb.AppendLine("    int PageSize = 20");
            sb.AppendLine(");");

            var filePath = Path.Combine(basePath, "Application", "DTOs", $"{entity.Name}Dtos.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateRepositories(string basePath, List<EntityDefinition> entities)
    {
        var interfaceContent = @"namespace Generated.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    IQueryable<T> Query();
    Task SaveChangesAsync();
}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Interfaces", "IRepository.cs"), interfaceContent);

        var implementationContent = @"using Microsoft.EntityFrameworkCore;
using Generated.Application.Interfaces;

namespace Generated.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await SaveChangesAsync();
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Repositories", "Repository.cs"), implementationContent);
    }

    private async Task GenerateServices(string basePath, List<EntityDefinition> entities)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using {entity.ProjectName}.Application.DTOs;");
            sb.AppendLine($"using {entity.ProjectName}.Application.Interfaces;");
            sb.AppendLine($"using {entity.ProjectName}.Domain.Entities;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine();
            sb.AppendLine($"namespace {entity.ProjectName}.Application.Services;");
            sb.AppendLine();
            sb.AppendLine($"public interface I{entity.Name}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    Task<IEnumerable<{entity.Name}Dto>> GetAllAsync();");
            sb.AppendLine($"    Task<{entity.Name}Dto?> GetByIdAsync(Guid id);");
            sb.AppendLine($"    Task<Guid> CreateAsync(Create{entity.Name}Dto dto);");
            sb.AppendLine($"    Task UpdateAsync(Guid id, Update{entity.Name}Dto dto);");
            sb.AppendLine($"    Task DeleteAsync(Guid id);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"public class {entity.Name}Service : I{entity.Name}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly IRepository<{entity.Name}> _repository;");
            sb.AppendLine();
            sb.AppendLine($"    public {entity.Name}Service(IRepository<{entity.Name}> repository)");
            sb.AppendLine("    {");
            sb.AppendLine("        _repository = repository;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<IEnumerable<{entity.Name}Dto>> GetAllAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entities = await _repository.GetAllAsync();");
            sb.AppendLine($"        return entities.Select(MapToDto);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<{entity.Name}Dto?> GetByIdAsync(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = await _repository.GetByIdAsync(id);");
            sb.AppendLine("        return entity == null ? null : MapToDto(entity);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<Guid> CreateAsync(Create{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = MapToEntity(dto);");
            sb.AppendLine($"        var created = await _repository.AddAsync(entity);");
            sb.AppendLine("        return created.Id;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task UpdateAsync(Guid id, Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = await _repository.GetByIdAsync(id);");
            sb.AppendLine("        if (entity == null) throw new Exception(\"Not found\");");
            sb.AppendLine("        UpdateEntity(entity, dto);");
            sb.AppendLine($"        await _repository.UpdateAsync(entity);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task DeleteAsync(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = await _repository.GetByIdAsync(id);");
            sb.AppendLine("        if (entity == null) throw new Exception(\"Not found\");");
            sb.AppendLine($"        await _repository.DeleteAsync(entity);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    private {entity.Name}Dto MapToDto({entity.Name} entity) => new(entity.Id, {GeneratePropertyMapping(entity)});");
            sb.AppendLine();
            sb.AppendLine($"    private {entity.Name} MapToEntity(Create{entity.Name}Dto dto) => new {{ {GenerateDtoMapping(entity)} }};");
            sb.AppendLine();
            sb.AppendLine($"    private void UpdateEntity({entity.Name} entity, Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            foreach (var prop in entity.Properties)
            {
                sb.AppendLine($"        entity.{prop.Name} = dto.{prop.Name};");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(basePath, "Application", "Services", $"{entity.Name}Service.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateControllers(string basePath, List<EntityDefinition> entities, List<QueryDefinition> queries)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
            sb.AppendLine($"using {entity.ProjectName}.Application.DTOs;");
            sb.AppendLine($"using {entity.ProjectName}.Application.Services;");
            sb.AppendLine();
            sb.AppendLine($"namespace {entity.ProjectName}.WebApi.Controllers;");
            sb.AppendLine();
            sb.AppendLine("[ApiController]");
            sb.AppendLine("[Route(\"api/[controller]\")]");
            sb.AppendLine("[Authorize]");
            sb.AppendLine($"public class {entity.Name}Controller : ControllerBase");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly I{entity.Name}Service _service;");
            sb.AppendLine();
            sb.AppendLine($"    public {entity.Name}Controller(I{entity.Name}Service service)");
            sb.AppendLine("    {");
            sb.AppendLine("        _service = service;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpGet]");
            sb.AppendLine($"    public async Task<IActionResult> GetAll()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.GetAllAsync();");
            sb.AppendLine("        return Ok(result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpGet(\"{id}\")]");
            sb.AppendLine($"    public async Task<IActionResult> GetById(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.GetByIdAsync(id);");
            sb.AppendLine("        return result == null ? NotFound() : Ok(result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpPost]");
            sb.AppendLine($"    public async Task<IActionResult> Create([FromBody] Create{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var id = await _service.CreateAsync(dto);");
            sb.AppendLine("        return CreatedAtAction(nameof(GetById), new { id }, new { id });");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpPut(\"{id}\")]");
            sb.AppendLine($"    public async Task<IActionResult> Update(Guid id, [FromBody] Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        await _service.UpdateAsync(id, dto);");
            sb.AppendLine("        return NoContent();");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpDelete(\"{id}\")]");
            sb.AppendLine($"    public async Task<IActionResult> Delete(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        await _service.DeleteAsync(id);");
            sb.AppendLine("        return NoContent();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(basePath, "WebApi", "Controllers", $"{entity.Name}Controller.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateDbContext(string basePath, string projectName, List<EntityDefinition> entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        foreach (var entity in entities)
        {
            sb.AppendLine($"using {entity.ProjectName}.Domain.Entities;");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Infrastructure.Data;");
        sb.AppendLine();
        sb.AppendLine("public class AppDbContext : DbContext");
        sb.AppendLine("{");
        sb.AppendLine("    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }");
        sb.AppendLine();

        foreach (var entity in entities)
        {
            sb.AppendLine($"    public DbSet<{entity.Name}> {entity.PluralName ?? entity.Name + "s"} {{ get; set; }}");
        }

        sb.AppendLine("}");

        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Data", "AppDbContext.cs"), sb.ToString());
    }

    private string GeneratePropertyMapping(EntityDefinition entity)
    {
        return string.Join(", ", entity.Properties.Select(p => $"entity.{p.Name}"));
    }

    private string GenerateDtoMapping(EntityDefinition entity)
    {
        return string.Join(", ", entity.Properties.Select(p => $"{p.Name} = dto.{p.Name}"));
    }
}

public class EntityDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PluralName { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class PropertyDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public List<string> Annotations { get; set; } = new();
}

public class QueryDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QuerySql { get; set; } = string.Empty;
    public string SourceEntity { get; set; } = string.Empty;
    public List<string> JoinEntities { get; set; } = new();
    public string GeneratedCode { get; set; } = string.Empty;
}
