using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using ArchoCybo.Services;
using ArchoCybo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ArchoCyboDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// HttpClient for calling API
// Create HttpClient per scope and prefer ApiBaseUrl from configuration; fallback to the current NavigationManager.BaseUri at runtime
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiBase = config["ApiBaseUrl"];
    // NavigationManager is available as a scoped service for Blazor server; use it when config is absent
    if (string.IsNullOrEmpty(apiBase))
    {
        var nav = sp.GetRequiredService<NavigationManager>();
        apiBase = nav.BaseUri;
    }
    return new HttpClient { BaseAddress = new Uri(apiBase) };
});

builder.Services.AddSingleton<TokenProvider>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
