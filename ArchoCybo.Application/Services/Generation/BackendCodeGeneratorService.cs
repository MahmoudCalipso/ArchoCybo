using System.Text;
using System.IO.Compression;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Domain.Entities;

namespace ArchoCybo.Application.Services.Generation;

public class BackendCodeGeneratorService
{
    public async Task<(string zipPath, string projectFolder)> GenerateBackendProject(
        string projectName,
        Guid userId,
        string userName,
        List<Entity> entities,
        List<CustomQuery> queries)
    {
        // Path: PROJECT-GEN-AI/<USER-ID>-(USER-Name)/<ProjectName>/
        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "PROJECT-GEN-AI");
        var userFolder = $"{userId}-{userName}";
        var projectBase = Path.Combine(rootPath, userFolder, projectName);
        var backendFolder = Path.Combine(projectBase, "Backend");

        if (Directory.Exists(backendFolder)) Directory.Delete(backendFolder, true);
        Directory.CreateDirectory(backendFolder);

        await GenerateProjectStructure(backendFolder, projectName, entities, queries);

        var zipPath = Path.Combine(projectBase, $"{projectName}-Backend.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        ZipFile.CreateFromDirectory(backendFolder, zipPath);

        return (zipPath, projectBase);
    }

    private async Task GenerateProjectStructure(string basePath, string projectName, List<Entity> entities, List<CustomQuery> queries)
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
        await GenerateEntities(basePath, projectName, entities);
        await GenerateDTOs(basePath, projectName, entities, queries);
        await GenerateRepositories(basePath, projectName);
        await GenerateUnitOfWork(basePath, projectName);
        await GenerateServices(basePath, projectName, entities);
        await GenerateControllers(basePath, projectName, entities, queries);
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
using {projectName}.Infrastructure.Data;
using {projectName}.Application.Interfaces;
using {projectName}.Infrastructure.Repositories;
using {projectName}.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

// Unit of Work & Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {{
        var key = Encoding.ASCII.GetBytes(builder.Configuration[""JwtSettings:SecretKey""] ?? ""SecretKeyMustBeLongerThanThisString123"");
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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
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

    private async Task GenerateEntities(string basePath, string projectName, List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.Domain.Entities;");
            sb.AppendLine();
            sb.AppendLine($"public class {entity.Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    public Guid Id {{ get; set; }} = Guid.NewGuid();");

            foreach (var field in entity.Fields)
            {
                if (field.Name == "Id") continue; // Skip Id as it's base
                var typeStr = MapClrType(field.DataType, field.IsNullable);
                sb.AppendLine($"    public {typeStr} {field.Name} {{ get; set; }}");
            }

            sb.AppendLine($"    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;");
            sb.AppendLine($"    public DateTime? UpdatedAt {{ get; set; }}");
            sb.AppendLine("}");

            var filePath = Path.Combine(basePath, "Domain", "Entities", $"{entity.Name}.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateDTOs(string basePath, string projectName, List<Entity> entities, List<CustomQuery> queries)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {projectName}.Application.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"public record {entity.Name}Dto(");
            sb.AppendLine("    Guid Id,");

            var props = entity.Fields.Where(f => f.Name != "Id").ToList();
            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var typeStr = MapClrType(props[i].DataType, props[i].IsNullable);
                sb.AppendLine($"    {typeStr} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine($"public record Create{entity.Name}Dto(");

            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var typeStr = MapClrType(props[i].DataType, props[i].IsNullable);
                sb.AppendLine($"    {typeStr} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine($"public record Update{entity.Name}Dto(");
            sb.AppendLine("    Guid Id,");

            for (int i = 0; i < props.Count; i++)
            {
                var comma = i < props.Count - 1 ? "," : "";
                var typeStr = MapClrType(props[i].DataType, props[i].IsNullable);
                sb.AppendLine($"    {typeStr} {props[i].Name}{comma}");
            }

            sb.AppendLine(");");

            var filePath = Path.Combine(basePath, "Application", "DTOs", $"{entity.Name}Dtos.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private string MapClrType(ArchoCybo.Domain.Enums.FieldDataType dt, bool isNullable)
    {
        string baseType = dt switch
        {
            ArchoCybo.Domain.Enums.FieldDataType.String => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Integer => "int",
            ArchoCybo.Domain.Enums.FieldDataType.Long => "long",
            ArchoCybo.Domain.Enums.FieldDataType.Decimal => "decimal",
            ArchoCybo.Domain.Enums.FieldDataType.Double => "double",
            ArchoCybo.Domain.Enums.FieldDataType.Boolean => "bool",
            ArchoCybo.Domain.Enums.FieldDataType.DateTime => "DateTime",
            ArchoCybo.Domain.Enums.FieldDataType.Date => "DateTime",
            ArchoCybo.Domain.Enums.FieldDataType.Time => "TimeSpan",
            ArchoCybo.Domain.Enums.FieldDataType.Guid => "Guid",
            ArchoCybo.Domain.Enums.FieldDataType.Json => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Binary => "byte[]",
            ArchoCybo.Domain.Enums.FieldDataType.Email => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Phone => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Url => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Color => "string",
            ArchoCybo.Domain.Enums.FieldDataType.File => "string",
            ArchoCybo.Domain.Enums.FieldDataType.Image => "string",
            _ => "string"
        };
        if (baseType == "string" || baseType == "byte[]") return baseType;
        return isNullable ? $"{baseType}?" : baseType;
    }
    private async Task GenerateUnitOfWork(string basePath, string projectName)
    {
        var interfaceContent = $@"namespace {projectName}.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}}";
        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Interfaces", "IUnitOfWork.cs"), interfaceContent);

        var implementationContent = $@"using {projectName}.Application.Interfaces;
using {projectName}.Infrastructure.Data;

namespace {projectName}.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{{
    private readonly AppDbContext _context;
    private readonly Dictionary<Type, object> _repositories;

    public UnitOfWork(AppDbContext context)
    {{
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }}

    public IRepository<T> Repository<T>() where T : class
    {{
        if (_repositories.ContainsKey(typeof(T)))
        {{
            return (IRepository<T>)_repositories[typeof(T)];
        }}

        var repository = new Repository<T>(_context);
        _repositories.Add(typeof(T), repository);
        return repository;
    }}

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {{
        return await _context.SaveChangesAsync(cancellationToken);
    }}

    public void Dispose()
    {{
        _context.Dispose();
    }}
}}";
        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Repositories", "UnitOfWork.cs"), implementationContent);
    }

    private async Task GenerateRepositories(string basePath, string projectName)
    {
        var interfaceContent = $@"namespace {projectName}.Application.Interfaces;

public interface IRepository<T> where T : class
{{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    IQueryable<T> Query();
    Task SaveChangesAsync();
}}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Interfaces", "IRepository.cs"), interfaceContent);

        var implementationContent = $@"using Microsoft.EntityFrameworkCore;
using {projectName}.Application.Interfaces;

namespace {projectName}.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {{
        _context = context;
        _dbSet = context.Set<T>();
    }}

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {{
        return await _dbSet.FindAsync(id);
    }}

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {{
        return await _dbSet.ToListAsync();
    }}

    public virtual async Task<T> AddAsync(T entity)
    {{
        await _dbSet.AddAsync(entity);
        await SaveChangesAsync();
        return entity;
    }}

    public virtual async Task<T> UpdateAsync(T entity)
    {{
        _dbSet.Update(entity);
        await SaveChangesAsync();
        return entity;
    }}

    public virtual async Task DeleteAsync(T entity)
    {{
        _dbSet.Remove(entity);
        await SaveChangesAsync();
    }}

    public virtual IQueryable<T> Query()
    {{
        return _dbSet.AsQueryable();
    }}

    public virtual async Task SaveChangesAsync()
    {{
        await _context.SaveChangesAsync();
    }}
}}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Repositories", "Repository.cs"), implementationContent);
    }

    private async Task GenerateServices(string basePath, string projectName, List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using {projectName}.Application.DTOs;");
            sb.AppendLine($"using {projectName}.Application.Interfaces;");
            sb.AppendLine($"using {projectName}.Domain.Entities;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.Application.Services;");
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
            sb.AppendLine($"    private {entity.Name} MapToEntity(Create{entity.Name}Dto dto) => new() {{ {GenerateDtoMapping(entity)} }};");
            sb.AppendLine();
            sb.AppendLine($"    private void UpdateEntity({entity.Name} entity, Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            foreach (var prop in entity.Fields.Where(f => f.Name != "Id"))
            {
                sb.AppendLine($"        entity.{prop.Name} = dto.{prop.Name};");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var filePath = Path.Combine(basePath, "Application", "Services", $"{entity.Name}Service.cs");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
    }

    private async Task GenerateControllers(string basePath, string projectName, List<Entity> entities, List<CustomQuery> queries)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
            sb.AppendLine($"using {projectName}.Application.DTOs;");
            sb.AppendLine($"using {projectName}.Application.Services;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.WebApi.Controllers;");
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

    private async Task GenerateDbContext(string basePath, string projectName, List<Entity> entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using {projectName}.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Infrastructure.Data;");
        sb.AppendLine();
        sb.AppendLine("public class AppDbContext : DbContext");
        sb.AppendLine("{");
        sb.AppendLine("    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }");
        sb.AppendLine();

        foreach (var entity in entities)
        {
            sb.AppendLine($"    public DbSet<{entity.Name}> {entity.Name}s {{ get; set; }}");
        }

        sb.AppendLine("}");

        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Data", "AppDbContext.cs"), sb.ToString());
    }

    private string GeneratePropertyMapping(Entity entity)
    {
        return string.Join(", ", entity.Fields.Where(f => f.Name != "Id").Select(p => $"entity.{p.Name}"));
    }

    private string GenerateDtoMapping(Entity entity)
    {
        return string.Join(", ", entity.Fields.Where(f => f.Name != "Id").Select(p => $"{p.Name} = dto.{p.Name}"));
    }
}
