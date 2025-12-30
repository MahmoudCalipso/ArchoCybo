using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArchoCybo.Pages.Authentication;

public partial class Register
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    private string fullName = string.Empty;
    private string email = string.Empty;
    private string username = string.Empty;
    private string password = string.Empty;
    private string confirmPassword = string.Empty;
    private string errorMessage = string.Empty;
    private bool isLoading = false;

    private async Task OnRegister()
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "All fields are required";
            return;
        }

        if (password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters";
            return;
        }

        if (password != confirmPassword)
        {
            errorMessage = "Passwords do not match";
            return;
        }

        isLoading = true;

        try
        {
            var registerDto = new
            {
                FullName = fullName,
                Email = email,
                Username = username,
                Password = password
            };

            var resp = await Http.PostAsJsonAsync("api/auth/register", registerDto);

            if (resp.IsSuccessStatusCode)
            {
                Snackbar.Add("Account created successfully! Redirecting to login...", Severity.Success);
                await Task.Delay(1500);
                Nav.NavigateTo("/login", true);
            }
            else
            {
                var errorContent = await resp.Content.ReadAsStringAsync();
                errorMessage = errorContent.Contains("already exists")
                    ? "Email or username already exists"
                    : "Registration failed. Please try again.";
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
