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
            "Domain/Common",
            "Application/DTOs",
            "Application/Interfaces",
            "Application/Services",
            "Application/Common",
            "Application/Models/Common",
            "Infrastructure/Data",
            "Infrastructure/Repositories",
            "Infrastructure/UnitOfWork",
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
        await GenerateEnterpriseComponents(basePath, projectName);
        await GenerateDockerFiles(basePath, projectName, entities.Count > 0 ? "SqlServer" : "None");
    }

    private async Task GenerateEnterpriseComponents(string basePath, string projectName)
    {
        // Domain Common
        await File.WriteAllTextAsync(Path.Combine(basePath, "Domain", "Common", "RepositoryAction.cs"), $@"namespace {projectName}.Domain.Common;
public enum RepositoryAction {{ Create, Update, Delete, Get }}");

        await File.WriteAllTextAsync(Path.Combine(basePath, "Domain", "Common", "RepositoryResult.cs"), $@"namespace {projectName}.Domain.Common;
public class RepositoryResult {{ public bool Success {{ get; init; }} public string Message {{ get; init; }} = string.Empty; public static RepositoryResult Ok(string message) => new() {{ Success = true, Message = message }}; public static RepositoryResult Fail(string message) => new() {{ Success = false, Message = message }}; }}
public class RepositoryResult<T> : RepositoryResult {{ public T? Data {{ get; init; }} public static RepositoryResult<T> Ok(T data, string message) => new() {{ Success = true, Data = data, Message = message }}; public new static RepositoryResult<T> Fail(string message) => new() {{ Success = false, Message = message }}; }}");

        await File.WriteAllTextAsync(Path.Combine(basePath, "Domain", "Common", "BaseFilter.cs"), $@"namespace {projectName}.Domain.Common;
public abstract class BaseFilter {{ public string? Search {{ get; set; }} }}");

        // Application Common
        var sb = new StringBuilder();
        sb.AppendLine($"using {projectName}.Domain.Common;");
        sb.AppendLine($"namespace {projectName}.Application.Common;");
        sb.AppendLine("public static class RepositoryMessageBuilder {");
        sb.AppendLine("    public static string Success(RepositoryAction action, string entityName) => action switch");
        sb.AppendLine("    {");
        sb.AppendLine("        RepositoryAction.Create => $\"YOUR ACTION CREATE ({entityName}) IS SUCCESSFULLY DONE\",");
        sb.AppendLine("        RepositoryAction.Update => $\"YOUR ACTION UPDATE ({entityName}) IS SUCCESSFULLY DONE\",");
        sb.AppendLine("        RepositoryAction.Delete => $\"YOUR ACTION DELETE ({entityName}) IS SUCCESSFULLY DONE\",");
        sb.AppendLine("        RepositoryAction.Get => $\"YOUR ACTION GET ({entityName}) IS SUCCESSFULLY DONE\",");
        sb.AppendLine("        _ => $\"YOUR ACTION {action} ({entityName}) IS SUCCESSFULLY DONE\"");
        sb.AppendLine("    };");
        sb.AppendLine("    public static string Failed(RepositoryAction action, string entityName) => action switch");
        sb.AppendLine("    {");
        sb.AppendLine("        RepositoryAction.Create => $\"YOUR ACTION CREATE ({entityName}) FAILED\",");
        sb.AppendLine("        RepositoryAction.Update => $\"YOUR ACTION UPDATE ({entityName}) FAILED\",");
        sb.AppendLine("        RepositoryAction.Delete => $\"YOUR ACTION DELETE ({entityName}) FAILED\",");
        sb.AppendLine("        RepositoryAction.Get => $\"YOUR ACTION GET ({entityName}) FAILED\",");
        sb.AppendLine("        _ => $\"YOUR ACTION FAILED\"");
        sb.AppendLine("    };");
        sb.AppendLine("    public static string NotFound(string entityName) => $\"SORRY THE {entityName} WAS NOT FOUND\";");
        sb.AppendLine("    public static string Error(RepositoryAction action, string entityName, string error) => $\"ERROR DURING {action} ON {entityName}: {error}\";");
        sb.AppendLine("}");
        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Common", "RepositoryMessageBuilder.cs"), sb.ToString());

        // Application Models Common
        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Models", "Common", "PaginationModels.cs"), $@"namespace {projectName}.Application.Models.Common;
public class PaginationRequest {{ public int PageNumber {{ get; set; }} = 1; public int PageSize {{ get; set; }} = 10; }}
public class PaginatedResult<T> {{ public IEnumerable<T> Items {{ get; set; }} = Enumerable.Empty<T>(); public int TotalCount {{ get; set; }} public int PageNumber {{ get; set; }} public int PageSize {{ get; set; }} }}");
    }
    private async Task GenerateDockerFiles(string basePath, string projectName, string dbType)
    {
        await GenerateDockerfile(basePath, projectName);
        await GenerateDockerCompose(basePath, projectName, dbType);
        await GenerateDockerIgnore(basePath);
    }

    private async Task GenerateDockerfile(string basePath, string projectName)
    {
        var content = $@"FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY [""{projectName}.csproj"", ""./""]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{projectName}.dll""]";
    
        await File.WriteAllTextAsync(Path.Combine(basePath, "Dockerfile"), content);
    }

    private async Task GenerateDockerCompose(string basePath, string projectName, string dbType)
    {
        var content = $@"version: '3.8'
services:
  api:
    build: .
    ports:
      - ""5000:80""
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database={projectName};User Id=sa;Password=Your_Password123;TrustServerCertificate=True
    depends_on:
      - db
  
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Your_Password123
    ports:
      - ""1433:1433""";
    
        await File.WriteAllTextAsync(Path.Combine(basePath, "docker-compose.yml"), content);
    }

    private async Task GenerateDockerIgnore(string basePath)
    {
        var content = @"**/.git
**/.gitignore
**/bin
**/obj
**/.vs
**/.vscode
**/PROJECT-GEN-AI
Dockerfile
docker-compose.yml";
        await File.WriteAllTextAsync(Path.Combine(basePath, ".dockerignore"), content);
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

    private async Task GenerateProgramCs(string basePath, string projectName, List<Entity> entities)
    {
        var servicesSb = new StringBuilder();
        foreach (var entity in entities)
        {
            servicesSb.AppendLine($"builder.Services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        }

        var content = $@"using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using {projectName}.Infrastructure.Data;
using {projectName}.Application.Interfaces;
using {projectName}.Application.Services;
using {projectName}.Infrastructure.Repositories;
using {projectName}.WebApi.Middleware;
using {projectName}.Domain.Common;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

// Unit of Work & Repositories
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
{servicesSb}

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {{
        var key = Encoding.ASCII.GetBytes(builder.Configuration[""""] ?? ""SecretKeyMustBeLongerThanThisString123"");
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
        var interfaceContent = $@"using {projectName}.Domain.Common;
namespace {projectName}.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{{
    IRepository<T, BaseFilter> Repository<T>() where T : class;
    IRepository<T, TFilter> Repository<T, TFilter>() where T : class where TFilter : BaseFilter;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}}";
        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Interfaces", "IUnitOfWork.cs"), interfaceContent);

        var implementationContent = $@"using {projectName}.Application.Interfaces;
using {projectName}.Infrastructure.Data;
using {projectName}.Infrastructure.Repositories;
using {projectName}.Domain.Common;

namespace {projectName}.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{{
    private readonly AppDbContext _context;
    private readonly Dictionary<(Type, Type), object> _repositories;

    public UnitOfWork(AppDbContext context)
    {{
        _context = context;
        _repositories = new Dictionary<(Type, Type), object>();
    }}

    public IRepository<T, BaseFilter> Repository<T>() where T : class
    {{
        return Repository<T, BaseFilter>();
    }}

    public IRepository<T, TFilter> Repository<T, TFilter>() where T : class where TFilter : BaseFilter
    {{
        var key = (typeof(T), typeof(TFilter));
        if (_repositories.ContainsKey(key))
        {{
            return (IRepository<T, TFilter>)_repositories[key];
        }}

        var repository = new EfRepository<T, TFilter>(_context);
        _repositories.Add(key, repository);
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
        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "UnitOfWork", "UnitOfWork.cs"), implementationContent);
    }

    private async Task GenerateRepositories(string basePath, string projectName)
    {
        var interfaceContent = $@"using {projectName}.Domain.Common;
using {projectName}.Application.Models.Common;
namespace {projectName}.Application.Interfaces;

public interface IRepository<T, TFilter> where T : class where TFilter : BaseFilter
{{
    Task<RepositoryResult<T>> GetByIdAsync(Guid id);
    Task<RepositoryResult<IEnumerable<T>>> GetAllAsync();
    Task<RepositoryResult<IEnumerable<T>>> GetAllAsNoTrackingAsync();
    Task<RepositoryResult<PaginatedResult<T>>> GetPagedAsync(TFilter filter, PaginationRequest pagination);
    IQueryable<T> Query();
    Task<RepositoryResult<T>> AddAsync(T entity);
    Task<RepositoryResult> UpdateAsync(T entity);
    Task<RepositoryResult> DeleteAsync(Guid id);
}}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Application", "Interfaces", "IRepository.cs"), interfaceContent);

        var implementationContent = $@"using Microsoft.EntityFrameworkCore;
using {projectName}.Application.Interfaces;
using {projectName}.Domain.Common;
using {projectName}.Application.Models.Common;
using {projectName}.Application.Common;

namespace {projectName}.Infrastructure.Repositories;

public class EfRepository<T, TFilter> : IRepository<T, TFilter> where T : class where TFilter : BaseFilter
{{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EfRepository(DbContext context)
    {{
        _context = context;
        _dbSet = context.Set<T>();
    }}

    public virtual async Task<RepositoryResult<T>> GetByIdAsync(Guid id)
    {{
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return RepositoryResult<T>.Fail(RepositoryMessageBuilder.NotFound(typeof(T).Name));
        return RepositoryResult<T>.Ok(entity, RepositoryMessageBuilder.Success(RepositoryAction.Get, typeof(T).Name));
    }}

    public virtual async Task<RepositoryResult<IEnumerable<T>>> GetAllAsync()
    {{
        var list = await _dbSet.ToListAsync();
        return RepositoryResult<IEnumerable<T>>.Ok(list, RepositoryMessageBuilder.Success(RepositoryAction.Get, typeof(T).Name));
    }}

    public virtual async Task<RepositoryResult<IEnumerable<T>>> GetAllAsNoTrackingAsync()
    {{
        var list = await _dbSet.AsNoTracking().ToListAsync();
        return RepositoryResult<IEnumerable<T>>.Ok(list, RepositoryMessageBuilder.Success(RepositoryAction.Get, typeof(T).Name));
    }}

    public virtual async Task<RepositoryResult<T>> AddAsync(T entity)
    {{
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return RepositoryResult<T>.Ok(entity, RepositoryMessageBuilder.Success(RepositoryAction.Create, typeof(T).Name));
    }}

    public virtual async Task<RepositoryResult> UpdateAsync(T entity)
    {{
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return RepositoryResult.Ok(RepositoryMessageBuilder.Success(RepositoryAction.Update, typeof(T).Name));
    }}

    public virtual async Task<RepositoryResult> DeleteAsync(Guid id)
    {{
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return RepositoryResult.Fail(RepositoryMessageBuilder.NotFound(typeof(T).Name));
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return RepositoryResult.Ok(RepositoryMessageBuilder.Success(RepositoryAction.Delete, typeof(T).Name));
    }}

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();

    public virtual async Task<RepositoryResult<PaginatedResult<T>>> GetPagedAsync(TFilter filter, PaginationRequest pagination)
    {{
        IQueryable<T> query = _dbSet.AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToListAsync();
        return RepositoryResult<PaginatedResult<T>>.Ok(new PaginatedResult<T> {{ Items = items, TotalCount = totalCount, PageNumber = pagination.PageNumber, PageSize = pagination.PageSize }}, RepositoryMessageBuilder.Success(RepositoryAction.Get, typeof(T).Name));
    }}
}}";

        await File.WriteAllTextAsync(Path.Combine(basePath, "Infrastructure", "Repositories", "EfRepository.cs"), implementationContent);
    }

    private async Task GenerateServices(string basePath, string projectName, List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using {projectName}.Application.DTOs;");
            sb.AppendLine($"using {projectName}.Application.Interfaces;");
            sb.AppendLine($"using {projectName}.Domain.Entities;");
            sb.AppendLine($"using {projectName}.Domain.Common;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine();
            sb.AppendLine($"namespace {projectName}.Application.Services;");
            sb.AppendLine();
            sb.AppendLine($"public interface I{entity.Name}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    Task<RepositoryResult<IEnumerable<{entity.Name}Dto>>> GetAllAsync();");
            sb.AppendLine($"    Task<RepositoryResult<{entity.Name}Dto>> GetByIdAsync(Guid id);");
            sb.AppendLine($"    Task<RepositoryResult<Guid>> CreateAsync(Create{entity.Name}Dto dto);");
            sb.AppendLine($"    Task<RepositoryResult> UpdateAsync(Guid id, Update{entity.Name}Dto dto);");
            sb.AppendLine($"    Task<RepositoryResult> DeleteAsync(Guid id);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"public class {entity.Name}Service : I{entity.Name}Service");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly IRepository<{entity.Name}, BaseFilter> _repository;");
            sb.AppendLine();
            sb.AppendLine($"    public {entity.Name}Service(IRepository<{entity.Name}, BaseFilter> repository)");
            sb.AppendLine("    {");
            sb.AppendLine("        _repository = repository;");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<RepositoryResult<IEnumerable<{entity.Name}Dto>>> GetAllAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _repository.GetAllAsync();");
            sb.AppendLine($"        if (!result.Success) return RepositoryResult<IEnumerable<{entity.Name}Dto>>.Fail(result.Message);");
            sb.AppendLine($"        return RepositoryResult<IEnumerable<{entity.Name}Dto>>.Ok(result.Data!.Select(MapToDto), result.Message);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<RepositoryResult<{entity.Name}Dto>> GetByIdAsync(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _repository.GetByIdAsync(id);");
            sb.AppendLine($"        if (!result.Success) return RepositoryResult<{entity.Name}Dto>.Fail(result.Message);");
            sb.AppendLine($"        return RepositoryResult<{entity.Name}Dto>.Ok(MapToDto(result.Data!), result.Message);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<RepositoryResult<Guid>> CreateAsync(Create{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var entity = MapToEntity(dto);");
            sb.AppendLine($"        var result = await _repository.AddAsync(entity);");
            sb.AppendLine($"        if (!result.Success) return RepositoryResult<Guid>.Fail(result.Message);");
            sb.AppendLine("        return RepositoryResult<Guid>.Ok(result.Data!.Id, result.Message);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<RepositoryResult> UpdateAsync(Guid id, Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _repository.GetByIdAsync(id);");
            sb.AppendLine("        if (!result.Success) return RepositoryResult.Fail(result.Message);");
            sb.AppendLine("        var entity = result.Data!;");
            sb.AppendLine("        UpdateEntity(entity, dto);");
            sb.AppendLine($"        return await _repository.UpdateAsync(entity);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public async Task<RepositoryResult> DeleteAsync(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return await _repository.DeleteAsync(id);");
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
            sb.AppendLine("        return result.Success ? Ok(result) : BadRequest(result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpGet(\"{id}\")]");
            sb.AppendLine($"    public async Task<IActionResult> GetById(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.GetByIdAsync(id);");
            sb.AppendLine("        return result.Success ? Ok(result) : NotFound(result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpPost]");
            sb.AppendLine($"    public async Task<IActionResult> Create([FromBody] Create{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.CreateAsync(dto);");
            sb.AppendLine("        if (!result.Success) return BadRequest(result);");
            sb.AppendLine("        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpPut(\"{id}\")]");
            sb.AppendLine($"    public async Task<IActionResult> Update(Guid id, [FromBody] Update{entity.Name}Dto dto)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.UpdateAsync(id, dto);");
            sb.AppendLine("        return result.Success ? Ok(result) : BadRequest(result);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [HttpGet(\"delete/{id}\")]"); // Simple delete for demo or [HttpDelete]
            sb.AppendLine($"    public async Task<IActionResult> Delete(Guid id)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var result = await _service.DeleteAsync(id);");
            sb.AppendLine("        return result.Success ? Ok(result) : BadRequest(result);");
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
