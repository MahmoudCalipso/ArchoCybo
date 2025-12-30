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
using Microsoft.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ArchoCyboDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Ensure DbContext base type can be resolved when repositories expect DbContext
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ArchoCyboDbContext>());

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Register TokenMessageHandler and configure HttpClient to automatically attach token
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddTransient<TokenMessageHandler>();

var apiBase = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBase)) apiBase = builder.Configuration.GetValue<string>("BaseAddress") ?? "";

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = string.IsNullOrEmpty(apiBase) ? new Uri(builder.Configuration.GetValue<string>("FrontendBaseUrl") ?? "http://localhost:5170") : new Uri(apiBase);
}).AddHttpMessageHandler<TokenMessageHandler>();

// Provide default HttpClient via factory so existing components injecting HttpClient work
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

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
