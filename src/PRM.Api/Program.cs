using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PRM.Api.Middleware;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Application.Services;
using PRM.Infrastructure.AI;
using PRM.Infrastructure.Auth;
using PRM.Infrastructure.Persistence;
using PRM.Infrastructure.Persistence.Repositories;
using PRM.Infrastructure.Scheduler;
using PRM.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// EF Core - SQLite
builder.Services.AddDbContext<PrmDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// HTTP client for AI providers
builder.Services.AddHttpClient();

// Repositories (DIP: services depend on interfaces, not concrete EF classes)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
builder.Services.AddScoped<ITimesheetRepository, TimesheetRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IHealthFlaggingService, HealthFlaggingService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// AI Provider Factory (Strategy + Factory Pattern)
builder.Services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

// Background Scheduler
builder.Services.AddHostedService<HealthFlaggingScheduler>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    db.Database.Migrate();
    await DbInitializer.SeedAsync(db);
}

app.Run();
