using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Services;
using ArchoCybo.Shared.Dialogs;

namespace ArchoCybo.Pages.User;

public partial class Users
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;

    private List<UserListData> users = new();
    private int page = 1;
    private int pageSize = 20;
    
    // Filters
    private string searchUsername = "";
    private string searchEmail = "";
    private DateTime? createdFrom;
    private DateTime? createdTo;
    
    private Guid? selectedUserId;
    private List<EndpointAccessDto>? endpointsForUser;
    private List<RoleSummaryDto> allRoles = new();
    private List<PermissionSummaryDto> allPermissions = new();

    protected override async Task OnInitializedAsync()
    {
        AttachToken();
        await LoadRoles();
        await LoadPermissions();
        await LoadUsers();
    }

    private void AttachToken()
    {
        if (!string.IsNullOrEmpty(TokenProvider.Token))
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenProvider.Token);
    }

    private async Task LoadUsers()
    {
        try
        {
            AttachToken();
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchUsername))
                queryParams.Add($"username={Uri.EscapeDataString(searchUsername)}");
            
            if (!string.IsNullOrWhiteSpace(searchEmail))
                queryParams.Add($"email={Uri.EscapeDataString(searchEmail)}");
            
            if (createdFrom.HasValue)
                queryParams.Add($"createdFrom={createdFrom.Value:yyyy-MM-dd}");
            
            if (createdTo.HasValue)
                queryParams.Add($"createdTo={createdTo.Value:yyyy-MM-dd}");

            var query = string.Join("&", queryParams);
            var resp = await Http.GetFromJsonAsync<PagedResult<UserListData>>($"api/Users?{query}");
            users = resp?.Items.ToList() ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading users: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadRoles()
    {
        try
        {
            allRoles = await Http.GetFromJsonAsync<List<RoleSummaryDto>>("api/Users/roles") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading roles: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadPermissions()
    {
        try
        {
            allPermissions = await Http.GetFromJsonAsync<List<PermissionSummaryDto>>("api/Users/permissions") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading permissions: {ex.Message}", Severity.Error);
        }
    }

    private void OnPageChanged(int newPage) { page = newPage + 1; _ = LoadUsers(); }
    private void OnPageSizeChanged(int newSize) { pageSize = newSize; _ = LoadUsers(); }
    
    private void ApplyFilters()
    {
        page = 1; // Reset to first page
        _ = LoadUsers();
    }
    
    private void ClearFilters()
    {
        searchUsername = "";
        searchEmail = "";
        createdFrom = null;
        createdTo = null;
        page = 1;
        _ = LoadUsers();
    }

    private async Task EditDetails(UserListData u)
    {
        var parameters = new DialogParameters
        {
            { "UserId", u.Id },
            { "Username", u.Username },
            { "Email", u.Email },
            { "IsActive", u.IsActive }
        };
        
        var dialog = DialogService.Show<EditUserDetailsDialog>("Edit User Details", parameters, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var result = await dialog.Result;
        
        if (!result.Cancelled)
        {
            await LoadUsers();
        }
    }

    private async Task EditRoles(UserListData u)
    {
        selectedUserId = u.Id;
        var roleIds = allRoles.Where(r => u.Roles.Contains(r.Name)).Select(r => r.Id).ToList();
        var parameters = new DialogParameters { { "AllRoles", allRoles }, { "SelectedRoleIds", roleIds } };
        var dialog = DialogService.Show<EditUserRolesDialog>("Edit Roles", parameters);
        var result = await dialog.Result;
        if (!result.Cancelled && result.Data is List<Guid> selected)
        {
            var ok = await Http.PutAsJsonAsync($"api/Users/{u.Id}/roles", selected);
            if (ok.IsSuccessStatusCode) { Snackbar.Add("Updated roles", Severity.Success); await LoadUsers(); }
            else { Snackbar.Add("Failed to update roles", Severity.Error); }
        }
    }

    private async Task EditPermissions(UserListData u)
    {
        selectedUserId = u.Id;
        var selectedPermIds = allPermissions.Where(p => u.Permissions.Contains(p.Name)).Select(p => p.Id).ToList();
        var parameters = new DialogParameters { { "AllPermissions", allPermissions }, { "SelectedPermissionIds", selectedPermIds } };
        var dialog = DialogService.Show<EditUserPermissionsDialog>("Edit Permissions", parameters);
        var result = await dialog.Result;
        if (!result.Cancelled && result.Data is List<Guid> selected)
        {
            var dto = new UpdateUserPermissionsDto { UserId = u.Id, AllowedPermissionIds = selected };
            var ok = await Http.PutAsJsonAsync($"api/Users/{u.Id}/permissions", dto);
            if (ok.IsSuccessStatusCode) { Snackbar.Add("Updated permissions", Severity.Success); await LoadUsers(); }
            else { Snackbar.Add("Failed to update permissions", Severity.Error); }
        }
    }

    private async Task ViewEndpoints(UserListData u)
    {
        selectedUserId = u.Id;
        try
        {
            AttachToken();
            endpointsForUser = await Http.GetFromJsonAsync<List<EndpointAccessDto>>($"api/Users/{u.Id}/endpoints") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading endpoints: {ex.Message}", Severity.Error);
        }
    }
}
