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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// MediatR (CQRS)
builder.Services.AddMediatR(typeof(LoginHandler).Assembly);

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

builder.Services.AddScoped<ArchoCybo.Application.Interfaces.IUnitOfWork, ArchoCybo.Infrastructure.UnitOfWork.UnitOfWork>();

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IQueryService, QueryService>();

// Project generator
builder.Services.AddScoped<ProjectGeneratorService>();

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
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

// Map Hangfire Dashboard and configure authorization if needed
app.MapHangfireDashboard();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public class JwtSettings
{
    public string Key { get; set; } = "secret";
    public string Issuer { get; set; } = "archocybo";
}
