using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using ArchoCybo.Services;
using ArchoCybo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Services.CodeViewer;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Infrastructure.Repositories;
using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Entities.CodeGeneration;
using ArchoCybo.Domain.Entities.Security;
using ArchoCybo.Infrastructure.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ArchoCyboDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiBase = config["ApiBaseUrl"];
    if (string.IsNullOrEmpty(apiBase))
    {
        var nav = sp.GetRequiredService<NavigationManager>();
        apiBase = nav.BaseUri;
    }
    return new HttpClient { BaseAddress = new Uri(apiBase) };
});

builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<CodeGenerationService>();

// Phase D: Code Viewer
builder.Services.AddScoped<ICodeViewerService, CodeViewerService>();

// Repository Pattern
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddAuthorization();

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
