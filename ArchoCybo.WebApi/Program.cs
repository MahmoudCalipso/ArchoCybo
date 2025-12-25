using Microsoft.EntityFrameworkCore;
using MediatR;
using ArchoCybo.Application.Features.Auth;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Services;
using ArchoCybo.Infrastructure.Data;
using ArchoCybo.Application.Interfaces.IServices.Background;
using ArchoCybo.Application.Services.Background;
using FluentValidation;
using FluentValidation.AspNetCore;
using ArchoCybo.WebApi.Hubs;
using ArchoCybo.WebApi.Services;
using ArchoCybo.Application.Services.Generation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options => 
{
    options.Filters.Add<ArchoCybo.WebApi.Filters.DynamicPermissionFilter>();
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ArchoCybo.Application.Validators.CreateProjectDtoValidator>();

// SignalR
builder.Services.AddSignalR();

// Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Add DbContext and unit of work
builder.Services.AddDbContext<ArchoCyboDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ArchoCybo.Application.Interfaces.IUnitOfWork>(sp =>
    new ArchoCybo.Infrastructure.UnitOfWork.UnitOfWork(
        sp.GetRequiredService<ArchoCyboDbContext>()));

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IQueryService, QueryService>();

// Project generator
builder.Services.AddScoped<ProjectGeneratorService>();
builder.Services.AddScoped<BackendCodeGeneratorService>();
builder.Services.AddScoped<EndpointDiscoveryService>();

// Background queue and worker (in-memory fallback)
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddHostedService<ProjectGenerationWorker>();

// Hangfire - persistent job queue
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Hangfire job service
builder.Services.AddScoped<HangfireJobService>();

// Notification publisher
builder.Services.AddSingleton<INotificationPublisher, NotificationPublisher>();

// Auth + Swagger auth config
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
var key = System.Text.Encoding.UTF8.GetBytes(jwtSettings?.Key ?? "secret");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Issuer,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArchoCyboDbContext>();
    db.Database.Migrate();
    var discovery = scope.ServiceProvider.GetRequiredService<EndpointDiscoveryService>();
    discovery.DiscoverEndpointsAsync().GetAwaiter().GetResult();
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

// Map Hangfire Dashboard and configure authorization if needed
app.MapHangfireDashboard();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Recurring endpoint discovery
Hangfire.RecurringJob.AddOrUpdate<EndpointDiscoveryService>("sync-endpoints", d => d.DiscoverEndpointsAsync(), Hangfire.Cron.Minutely);

app.Run();

public class JwtSettings
{
    public string Key { get; set; } = "secret";
    public string Issuer { get; set; } = "archocybo";
}

