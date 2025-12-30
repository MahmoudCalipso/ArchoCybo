using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ArchoCybo.Services;

namespace ArchoCybo.Pages.Authentication;

public partial class Login
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public AuthStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    private string username = "admin";
    private string password = "ChangeMe123!";
    private string errorMessage = string.Empty;
    private bool isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            Nav.NavigateTo("/");
        }
    }

    private async Task OnLogin()
    {
        errorMessage = string.Empty;
        isLoading = true;

        try
        {
            var resp = await Http.PostAsJsonAsync("api/auth/login", new { Username = username, Password = password });

            if (resp.IsSuccessStatusCode)
            {
                var j = await resp.Content.ReadFromJsonAsync<JsonElement>();
                var token = j.GetProperty("token").GetString();

                if (!string.IsNullOrEmpty(token))
                {
                    await AuthStateProvider.MarkUserAsAuthenticated(token!, username); // token is checked for null/empty
                    Snackbar.Add("Login successful!", Severity.Success);
                    Nav.NavigateTo("/");
                }
            }
            else
            {
                errorMessage = "Invalid username or password";
            }
        }
        catch (Exception)
        {
            errorMessage = "Connection error. Please try again.";
            Snackbar.Add(errorMessage, Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }
}
