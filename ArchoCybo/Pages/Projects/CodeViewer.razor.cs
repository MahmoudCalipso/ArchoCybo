using Microsoft.AspNetCore.Components;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces.IServices;
using BlazorMonaco.Editor;
using MudBlazor;
using Microsoft.JSInterop;

namespace ArchoCybo.Pages.Projects;

public partial class CodeViewer : ComponentBase
{
    [Parameter] public Guid ProjectId { get; set; }
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private FileNodeDto? rootNode;
    private FileNodeDto? _selectedNode;
    private FileNodeDto? selectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode != value)
            {
                _selectedNode = value;
                _ = OnNodeSelected(_selectedNode);
            }
        }
    }

    private StandaloneCodeEditor _editor = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            rootNode = await CodeViewerService.GetProjectFileTreeAsync(ProjectId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load file tree: {ex.Message}", Severity.Error);
        }
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "csharp",
            ReadOnly = true,
            Theme = "vs-dark",
            Value = "// Select a file to view code",
            Minimap = new EditorMinimapOptions { Enabled = true },
            FontSize = 14,
            RenderWhitespace = "all",
            ScrollBeyondLastLine = false
        };
    }

    private async Task OnNodeSelected(FileNodeDto? node)
    {
        if (node == null || node.IsDirectory) return;

        try
        {
            var content = await CodeViewerService.GetFileContentAsync(ProjectId, node.RelativePath);
            await _editor.SetValue(content);
            
            var language = GetLanguage(node.Extension);
            var model = await _editor.GetModel();
            if (model != null)
            {
                await Global.SetModelLanguage(JSRuntime, model, language);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load file content: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnEditorInit()
    {
        // Initial setup if needed
    }

    private string GetLanguage(string? extension)
    {
        return extension?.ToLower() switch
        {
            ".cs" => "csharp",
            ".json" => "json",
            ".sql" => "sql",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".xml" or ".csproj" => "xml",
            ".css" => "css",
            ".js" => "javascript",
            ".razor" => "html", // Monaco doesn't have a perfect "razor" language by default sometimes, html/csharp mix
            _ => "plaintext"
        };
    }

    private string GetFileIcon(string? extension)
    {
        return extension?.ToLower() switch
        {
            ".cs" => Icons.Material.Filled.Code,
            ".csproj" => Icons.Material.Filled.Settings,
            ".json" => Icons.Material.Filled.DataObject,
            ".sql" => Icons.Material.Filled.Storage,
            ".yaml" or ".yml" => Icons.Material.Filled.SettingsInputComposite,
            ".md" => Icons.Material.Filled.Description,
            _ => Icons.Material.Filled.InsertDriveFile
        };
    }
}
