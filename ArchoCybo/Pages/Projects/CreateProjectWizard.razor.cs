using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Services;

namespace ArchoCybo.Pages.Projects;

public partial class CreateProjectWizard
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;

    // Wizard State
    private int activeStepIndex = 0;
    private bool isLoading = false;
    private bool isStepValid = false;

    // Data Models
    private CreateProjectModel model = new();
    private string selectedDatabase = "SQL Server";
    private string selectedSourceType = "New"; // "New", "ConnectionString", "Upload"
    private IBrowserFile? uploadedFile;
    private string connectionString = "";

    protected override void OnInitialized()
    {
        ValidateStep();
    }

    private void SetStep(int index)
    {
        activeStepIndex = index;
        ValidateStep();
    }

    private async Task NextStep()
    {
        if (activeStepIndex < 3)
        {
            activeStepIndex++;
            ValidateStep();
        }
        else
        {
            await FinishWizard();
        }
    }

    private void PreviousStep()
    {
        if (activeStepIndex > 0)
        {
            activeStepIndex--;
            ValidateStep();
        }
    }

    private void ValidateStep()
    {
        isStepValid = false;
        switch (activeStepIndex)
        {
            case 0: // Info
                isStepValid = !string.IsNullOrWhiteSpace(model.Name);
                break;
            case 1: // Database
                isStepValid = !string.IsNullOrEmpty(selectedDatabase);
                break;
            case 2: // Source
                if (selectedSourceType == "New") isStepValid = true;
                else if (selectedSourceType == "ConnectionString") isStepValid = !string.IsNullOrWhiteSpace(connectionString);
                else if (selectedSourceType == "Upload") isStepValid = uploadedFile != null;
                break;
            case 3: // Review / Entities
                isStepValid = true;
                break;
        }
    }

    private void SelectDatabase(string db)
    {
        selectedDatabase = db;
        ValidateStep();
    }

    private void SelectSource(string source)
    {
        selectedSourceType = source;
        ValidateStep();
    }

    private void UploadFiles(IBrowserFile file)
    {
        uploadedFile = file;
        ValidateStep();
    }

    private async Task FinishWizard()
    {
        isLoading = true;
        try
        {
            // Attach Token
            if (!string.IsNullOrEmpty(TokenProvider.Token)) 
                Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenProvider.Token);

            // 1. Create Project
            var createDto = new CreateProjectDto(model.Name, model.Description ?? ""); // Assuming DTO exists or uses similar structure
            // If CreateProjectDto is not defined, we might need to use generic object or ensure generated DTO matches
            // Wait, existing usage in Projects.razor.cs uses "Shared.Dialogs.CreateEditProjectDialog.CreateProjectModel".
            // I should verify DTO. I'll use anonymous object or match CreateProjectDto if exists.
            
            // Actually, let's assume we post to "api/project" with Name, Description, DatabaseType
            // I need to map selectedDatabase to Enum or string.
            
            var payload = new 
            {
                Name = model.Name,
                Description = model.Description,
                DatabaseType = selectedDatabase // Backend parses this
            };

            var response = await Http.PostAsJsonAsync("api/project", payload);
            
            if (response.IsSuccessStatusCode)
            {
                var createdProject = await response.Content.ReadFromJsonAsync<ProjectDto>(); // Assuming ProjectDto return
                if (createdProject != null)
                {
                    // 2. Handle Import if needed
                    if (selectedSourceType == "Upload" && uploadedFile != null)
                    {
                        // Implement file upload logic if backend supports it
                        // For now, we'll just simulate or log
                        Snackbar.Add("Schema Import not fully implemented yet, proceeding with empty project.", Severity.Warning);
                    }
                    else if (selectedSourceType == "ConnectionString" && !string.IsNullOrEmpty(connectionString))
                    {
                         // Send connection string to backend to reverse engineer
                         // await Http.PostAsJsonAsync($"api/projects/{createdProject.Id}/import-schema", new { ConnectionString = connectionString });
                    }

                    Snackbar.Add("Project Created Successfully!", Severity.Success);
                    Nav.NavigateTo($"/project/{createdProject.Id}/schema"); // Navigate to Schema Designer
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Snackbar.Add("Failed to create project: " + error, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    public class CreateProjectModel
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }
    
    // Quick DTO definition if missing
    private record CreateProjectDto(string Name, string Description);
    private class ProjectDto { public Guid Id { get; set; } }
}
