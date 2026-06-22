using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRM.Api.Middleware;
using PRM.Application.DTOs.Common;
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

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(er => new ValidationErrorDto(e.Key, er.ErrorMessage)))
                .ToList();

            var response = new ApiResponse(false, "Validation Failed", errors);
            return new BadRequestObjectResult(response);
        };
    });

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PRM System API",
        Version = "v2.0",
        Description = "Project Resource Management System - Complete API with RBAC, Allocations, Timesheets, and Project Management",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@prm.local"
        },
        License = new OpenApiLicense
        {
            Name = "Proprietary",
            Url = new Uri("https://example.com/license")
        }
    });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT token (without 'Bearer ' prefix)",
        In = ParameterLocation.Header,
        Name = "Authorization"
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments for API descriptions (optional - requires project XML doc generation)
    var xmlFile = "PRM.Api.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        opts.IncludeXmlComments(xmlPath);
});

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
builder.Services.AddScoped<IEmployeeAccessStatusRepository, EmployeeAccessStatusRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();

// Generic repository for new entities
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITimesheetAccessService, TimesheetAccessService>();
builder.Services.AddScoped<IProjectRiskNotificationService, ProjectRiskNotificationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// AI Provider Factory (Strategy + Factory Pattern)
builder.Services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

// Background Scheduler
builder.Services.AddHostedService<HealthFlaggingScheduler>();
builder.Services.AddHostedService<TimesheetAccessScheduler>();

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(opts =>
    {
        opts.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/api-docs/v1/swagger.json", "PRM System API v2.0");
        opts.RoutePrefix = "swagger";
        opts.DefaultModelsExpandDepth(2);
        opts.DefaultModelExpandDepth(2);
        opts.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        opts.EnableValidator();
        opts.EnableTryItOutByDefault();
    });
}

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

public partial class Program { }
