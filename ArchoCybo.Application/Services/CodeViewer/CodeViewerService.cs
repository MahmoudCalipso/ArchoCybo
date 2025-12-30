using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Application.Services.CodeViewer;

public class CodeViewerService : ICodeViewerService
{
    private readonly IRepository<GeneratedProject, BaseFilter> _projectRepo;
    private readonly IRepository<User, BaseFilter> _userRepo;

    public CodeViewerService(
        IRepository<GeneratedProject, BaseFilter> projectRepo,
        IRepository<User, BaseFilter> userRepo)
    {
        _projectRepo = projectRepo;
        _userRepo = userRepo;
    }

    public async Task<FileNodeDto> GetProjectFileTreeAsync(Guid projectId)
    {
        var projectPath = await GetProjectPath(projectId);
        if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException("Project folder not found.");
        }

        return BuildTree(projectPath, projectPath);
    }

    public async Task<string> GetFileContentAsync(Guid projectId, string relativePath)
    {
        var projectPath = await GetProjectPath(projectId);
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new DirectoryNotFoundException("Project folder not found.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(projectPath, relativePath));
        
        // Security check: ensure the file is within the project folder
        if (!fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Cannot access files outside the project directory.");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found.");
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    private async Task<string> GetProjectPath(Guid projectId)
    {
        var projectRes = await _projectRepo.GetByIdAsync(projectId);
        if (!projectRes.Success || projectRes.Data == null) return string.Empty;

        var userRes = await _userRepo.GetByIdAsync(projectRes.Data.OwnerUserId);
        var userName = userRes.Success && userRes.Data != null ? userRes.Data.Username : "Unknown";

        var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "PROJECT-GEN-AI");
        var userFolder = $"{projectRes.Data.OwnerUserId}-{userName}";
        return Path.Combine(rootPath, userFolder, projectRes.Data.Name, "Backend");
    }

    private FileNodeDto BuildTree(string rootPath, string currentPath)
    {
        var directoryInfo = new DirectoryInfo(currentPath);
        var node = new FileNodeDto
        {
            Name = directoryInfo.Name,
            RelativePath = Path.GetRelativePath(rootPath, currentPath).Replace('\\', '/'),
            IsDirectory = true
        };

        foreach (var dir in directoryInfo.GetDirectories())
        {
            // Ignore common noise folders if any
            if (dir.Name == "bin" || dir.Name == "obj" || dir.Name == ".git") continue;
            node.Children.Add(BuildTree(rootPath, dir.FullName));
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            node.Children.Add(new FileNodeDto
            {
                Name = file.Name,
                RelativePath = Path.GetRelativePath(rootPath, file.FullName).Replace('\\', '/'),
                IsDirectory = false,
                Extension = file.Extension
            });
        }

        // Sort: directories first, then files
        node.Children = node.Children
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name)
            .ToHashSet();

        return node;
    }
}
