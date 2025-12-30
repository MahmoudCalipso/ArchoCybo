using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Services;
using ArchoCybo.Shared;
using ArchoCybo.Shared.Dialogs;

namespace ArchoCybo.Pages.Projects;

public partial class Projects
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    private string search = string.Empty;
    private int page = 1;
    private int pageSize = 10;
    private int totalCount = 0;

    private List<ProjectListItem> projects = new();
    private ProjectListItem? selected;

    private string? artifactUrl;

    protected override async Task OnInitializedAsync()
    {
        await LoadProjects();
    }

    private async Task LoadProjects()
    {
        try
        {
            AttachTokenIfNeeded();
            // Note: Update API path if needed, assuming /api/project/paged exists
            var res = await Http.GetFromJsonAsync<PagedResult<ProjectListItemDto>>($"api/project/paged?page={page}&pageSize={pageSize}&q={Uri.EscapeDataString(search)}");
            if (res != null)
            {
                projects = res.Items.Select(x => new ProjectListItem { Id = x.Id, Name = x.Name, DatabaseType = x.DatabaseType.ToString(), Status = x.Status.ToString(), CreatedAt = x.CreatedAt }).ToList();
                totalCount = res.TotalCount;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load projects: " + ex.Message, Severity.Error);
            projects = new List<ProjectListItem>();
            totalCount = 0;
        }
    }

    private void AttachTokenIfNeeded()
    {
        if (!string.IsNullOrEmpty(TokenProvider.Token)) Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenProvider.Token);
        else Http.DefaultRequestHeaders.Authorization = null;
    }

    private void OpenCreate()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters { { "Model", new CreateEditProjectDialog.CreateProjectModel() } };
        var dlg = DialogService.Show<CreateEditProjectDialog>("New Project", parameters, options);
        _ = dlg.Result.ContinueWith(async t => { if (!t.IsFaulted && t.Result.Data is true) await LoadProjects(); });
    }

    private void ShowDetails(ProjectListItem p)
    {
        selected = p;
        artifactUrl = null;
    }

    private void OpenEdit()
    {
        if (selected == null) return;
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters { { "Model", new CreateEditProjectDialog.CreateProjectModel { Name = selected.Name, Description = selected.Description, DatabaseType = Enum.Parse<CreateEditProjectDialog.DatabaseType>(selected.DatabaseType) } } };
        var dlg = DialogService.Show<CreateEditProjectDialog>("Edit Project", parameters, options);
        _ = dlg.Result.ContinueWith(async t => { if (!t.IsFaulted && t.Result.Data is true) await LoadProjects(); });
    }

    private async Task GenerateProject(Guid id)
    {
        try
        {
            AttachTokenIfNeeded();
            var resp = await Http.PostAsync($"api/generation/{id}/generate", null);
            if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                Snackbar.Add("Generation started", Severity.Success);
                await LoadProjects();
            }
            else
            {
                var t = await resp.Content.ReadAsStringAsync();
                Snackbar.Add("Failed to start generation: " + t, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error starting generation: " + ex.Message, Severity.Error);
        }
    }

    private async Task ConfirmDelete()
    {
        if (selected == null) return;
        var parameters = new DialogParameters { { "ContentText", $"Are you sure you want to delete '{selected.Name}'?" }, { "ButtonText", "Delete" }, { "Color", Color.Error } };
        var options = new DialogOptions { CloseButton = true };
        var dlg = DialogService.Show<DialogConfirm>("Confirm", parameters, options);
        var result = await dlg.Result;
        if (!result.Cancelled)
        {
            await DeleteSelected();
        }
    }

    private async Task DeleteSelected()
    {
        if (selected == null) return;
        try
        {
            AttachTokenIfNeeded();
            var resp = await Http.DeleteAsync($"api/project/{selected.Id}");
            if (resp.IsSuccessStatusCode)
            {
                Snackbar.Add("Project deleted", Severity.Success);
                selected = null;
                await LoadProjects();
            }
            else
            {
                var t = await resp.Content.ReadAsStringAsync();
                Snackbar.Add("Failed to delete: " + t, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error deleting project: " + ex.Message, Severity.Error);
        }
    }

    private void OpenPushToGit()
    {
        if (selected == null) return;
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters 
        { 
            { "ProjectName", selected.Name },
            { "ProjectId", selected.Id }
        };
        DialogService.Show<ArchoCybo.Shared.Components.PushToGitDialog>("Push to Git", parameters, options);
    }

    private void OpenLivePreview()
    {
        if (selected == null) return;
        Nav.NavigateTo($"/projects/{selected.Id}/preview");
    }

    private void OpenCodeViewer()
    {
        if (selected == null) return;
        Nav.NavigateTo($"/projects/{selected.Id}/code");
    }

    private void OnSearchChanged(string value)
    {
        search = value;
        _ = LoadProjects();
    }

    // Models
    public class ProjectListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DatabaseType { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }
}
