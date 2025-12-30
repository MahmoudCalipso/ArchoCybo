using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Services;
using ArchoCybo.Shared.Dialogs;

namespace ArchoCybo.Pages.Admin;

public partial class Roles
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;

    private List<RoleSummaryDto> roles = new();

    protected override async Task OnInitializedAsync()
    {
        AttachToken();
        await LoadRoles();
    }

    private void AttachToken()
    {
        if (!string.IsNullOrEmpty(TokenProvider.Token))
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenProvider.Token);
    }

    private async Task LoadRoles()
    {
        try
        {
            roles = await Http.GetFromJsonAsync<List<RoleSummaryDto>>("api/Users/roles") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load roles: " + ex.Message, Severity.Error);
        }
    }

    private async Task ManagePermissions(RoleSummaryDto role)
    {
        var parameters = new DialogParameters { { "RoleId", role.Id }, { "RoleName", role.Name } };
        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
        var dialog = DialogService.Show<ManageRolePermissionsDialog>("Manage Role Permissions", parameters, options);
        var result = await dialog.Result;

        if (!result.Cancelled)
        {
            // Reload if needed
        }
    }
}
