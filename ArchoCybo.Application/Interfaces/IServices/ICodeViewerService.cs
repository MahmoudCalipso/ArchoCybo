using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Interfaces.IServices;

public interface ICodeViewerService
{
    Task<FileNodeDto> GetProjectFileTreeAsync(Guid projectId);
    Task<string> GetFileContentAsync(Guid projectId, string relativePath);
}
