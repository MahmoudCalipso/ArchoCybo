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
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONTROLLERS & JSON ---
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

// --- 2. SWAGGER CONFIG (Auth Support) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "ArchoCybo API", Version = "v1" });
    
    // JWT Authorize Button configuration
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste your JWT token here",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- 3. CORS & SIGNALR (Fixes "Failed to Fetch") ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Essential for SignalR Hubs
    });
});

builder.Services.AddSignalR();

// --- 4. DATA & AUTHENTICATION (Using your AppSettings) ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<ArchoCyboDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ArchoCybo.Application.Interfaces.IUnitOfWork>(sp =>
    new ArchoCybo.Infrastructure.UnitOfWork.UnitOfWork(sp.GetRequiredService<ArchoCyboDbContext>()));

// Pulling values from your uploaded JSON
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Default_Fallback_Key_32_Characters_Long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ArchoCybo";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication("Bearer")
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

// --- 5. AUTHORIZATION (Dynamic Role/Type List) ---
builder.Services.AddAuthorization(options =>
{
    // You can add any custom 'Types' or Roles here
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin", "SuperUser", "Developer", "Manager"));
});

// --- 6. APPLICATION SERVICES ---
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<ProjectGeneratorService>();
builder.Services.AddScoped<BackendCodeGeneratorService>();
builder.Services.AddScoped<EndpointDiscoveryService>();
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddHostedService<ProjectGenerationWorker>();

builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<HangfireJobService>();
builder.Services.AddSingleton<INotificationPublisher, NotificationPublisher>();

var app = builder.Build();

// --- 7. MIDDLEWARE PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            // Forces Swagger to match the exact URL/Port in your browser
            swaggerDoc.Servers = new List<OpenApiServer> 
            { 
                new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" } 
            };
        });
    });
    app.UseSwaggerUI();
}

// Order is critical: CORS -> Auth -> Routing
app.UseCors("Frontend");

// app.UseHttpsRedirection(); // Keep commented out for pure HTTP local dev

app.UseAuthentication();
app.UseAuthorization();

// 8. DB INITIALIZATION & SEEDING
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArchoCyboDbContext>();
    db.Database.Migrate();
    var discovery = scope.ServiceProvider.GetRequiredService<EndpointDiscoveryService>();
    discovery.DiscoverEndpointsAsync().GetAwaiter().GetResult();
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

app.UseHangfireDashboard();
app.MapHangfireDashboard();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

Hangfire.RecurringJob.AddOrUpdate<EndpointDiscoveryService>(
    "sync-endpoints", 
    d => d.DiscoverEndpointsAsync(), 
    Hangfire.Cron.Minutely);

app.Run();

// Matches your AppSettings structure
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}