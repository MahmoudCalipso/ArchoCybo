namespace ArchoCybo.Application.DTOs;

public class FileNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public HashSet<FileNodeDto> Children { get; set; } = new();
    public string? Extension { get; set; }
}
