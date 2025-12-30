using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Json;

namespace ArchoCybo.Shared.Dialogs;

public partial class EditUserDetailsDialog
{
    [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;
    
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public string Username { get; set; } = "";
    [Parameter] public string Email { get; set; } = "";
    [Parameter] public bool IsActive { get; set; }

    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    private bool isLoading = false;

    private async Task SaveChanges()
    {
        isLoading = true;
        try
        {
            var dto = new
            {
                Username,
                Email,
                IsActive
            };

            var response = await Http.PutAsJsonAsync($"api/Users/{UserId}/details", dto);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("User details updated successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Snackbar.Add($"Failed to update: {error}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
