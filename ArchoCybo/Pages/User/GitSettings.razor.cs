using Microsoft.AspNetCore.Components;
using ArchoCybo.Domain.Entities.Security;
using MudBlazor;
using System.Net.Http.Json;

namespace ArchoCybo.Pages.User;

public partial class GitSettings
{
    private bool isGitHubConnected = false;
    private string? githubUsername;

    protected override async Task OnInitializedAsync()
    {
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        try
        {
            // In a real scenario, we'd fetch the user's Git configurations
            // For now, let's pretend we're checking if GitHub is connected
            // var configs = await Http.GetFromJsonAsync<List<UserGitConfiguration>>("api/Git/configurations");
            // isGitHubConnected = configs.Any(c => c.Platform == GitPlatform.GitHub);
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    private async Task Connect(GitPlatform platform)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<AuthUrlResponse>($"api/Git/auth-url/{platform}?state={Guid.NewGuid()}");
            if (response != null && !string.IsNullOrEmpty(response.Url))
            {
                Navigation.NavigateTo(response.Url);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error starting connection: {ex.Message}", Severity.Error);
        }
    }

    private async Task Disconnect(GitPlatform platform)
    {
        // Implementation for disconnecting platform
        Snackbar.Add($"{platform} disconnected.", Severity.Info);
        if (platform == GitPlatform.GitHub) isGitHubConnected = false;
    }

    public class AuthUrlResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
