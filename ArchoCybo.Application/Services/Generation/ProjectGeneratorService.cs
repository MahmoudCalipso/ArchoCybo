using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities.CodeGeneration;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ArchoCybo.Application.Services.Generation;

public class ProjectGeneratorService
{
    private readonly IUnitOfWork _uow;
    private readonly string _rootPath;

    public ProjectGeneratorService(IUnitOfWork uow)
    {
        _uow = uow;
        _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "PROJECTS-IA-FROM-USERS");
        if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> GenerateAsync(Guid projectId)
    {
        var repo = _uow.Repository<GeneratedProject>();
        var project = await repo.GetByIdAsync(projectId);
        if (project == null) throw new Exception("Project not found");

        // Load user for folder name
        var userRepo = _uow.Repository<ArchoCybo.Domain.Entities.Security.User>();
        var user = await userRepo.GetByIdAsync(project.OwnerUserId);
        var ownerFolder = user != null ? $"{user.FirstName}_{project.OwnerUserId}" : project.OwnerUserId.ToString();

        var projectFolder = Path.Combine(_rootPath, ownerFolder, project.Name);
        Directory.CreateDirectory(projectFolder);

        // Create backend/frontend subfolders and simple sample files
        var backend = Path.Combine(projectFolder, "backend");
        var frontend = Path.Combine(projectFolder, "frontend");
        Directory.CreateDirectory(backend);
        Directory.CreateDirectory(frontend);

        // write placeholder backend Program.cs
        await File.WriteAllTextAsync(Path.Combine(backend, "Program.cs"), "// Backend entry - generated\n");
        await File.WriteAllTextAsync(Path.Combine(frontend, "README.md"), "# Frontend - generated\n");

        // create Dockerfiles
        await File.WriteAllTextAsync(Path.Combine(backend, "Dockerfile"), GetBackendDockerfile(project.DatabaseType));
        await File.WriteAllTextAsync(Path.Combine(frontend, "Dockerfile"), GetFrontendDockerfile());

        // create docker-compose
        await File.WriteAllTextAsync(Path.Combine(projectFolder, "docker-compose.yml"), GetDockerCompose(project.DatabaseType, project.Name));

        // create zip
        var zipPath = Path.Combine(Directory.GetCurrentDirectory(), $"{project.Name}_{projectId}.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(projectFolder, zipPath, CompressionLevel.Optimal, true);

        return zipPath;
    }

    private string GetBackendDockerfile(ArchoCybo.Domain.Enums.DatabaseType dbType)
    {
        // simple dotnet dockerfile
        return @"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY . .
ENTRYPOINT [""dotnet"", ""ArchoCybo.WebApi.dll""]";
    }

    private string GetFrontendDockerfile()
    {
        return @"FROM node:20-alpine AS build
WORKDIR /app
COPY . .
RUN npm install && npm run build
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html";
    }

    private string GetDockerCompose(ArchoCybo.Domain.Enums.DatabaseType dbType, string projectName)
    {
        var dbService = dbType switch
        {
            ArchoCybo.Domain.Enums.DatabaseType.SqlServer => "image: mcr.microsoft.com/mssql/server:2022-latest",
            ArchoCybo.Domain.Enums.DatabaseType.MySQL => "image: mysql:8",
            ArchoCybo.Domain.Enums.DatabaseType.PostgreSQL => "image: postgres:15",
            ArchoCybo.Domain.Enums.DatabaseType.SQLite => "image: alpine",
            _ => "image: mcr.microsoft.com/mssql/server:2022-latest"
        };

        return $"version: '3.8'\nservices:\n  db:\n    {dbService}\n  backend:\n    build: ./backend\n    ports:\n      - '5000:80'\n  frontend:\n    build: ./frontend\n    ports:\n      - '3000:80'";
    }
}
