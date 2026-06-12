using Microsoft.EntityFrameworkCore;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(PrmDbContext db)
    {
        if (await db.Users.AnyAsync())
            return; // Already seeded

        // Create roles
        var adminRole = new Role { Name = "Admin" };
        var managerRole = new Role { Name = "Manager" };
        var employeeRole = new Role { Name = "Employee" };

        await db.Roles.AddAsync(adminRole);
        await db.Roles.AddAsync(managerRole);
        await db.Roles.AddAsync(employeeRole);
        await db.SaveChangesAsync();

        // Create admin user
        var adminUser = new User
        {
            FullName = "System Admin",
            Email = "admin@prm.local",
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
            RoleId = adminRole.Id,
            IsActive = true,
            ForcePasswordChange = true  // must change on first login
        };

        await db.Users.AddAsync(adminUser);

        var defaultConfig = new SystemConfig
        {
            LlmProvider = "Gemini",
            LlmApiKey = string.Empty,
            SchedulerIntervalHours = 4,
            MaxWeeklyHours = 40
        };
        await db.SystemConfigs.AddAsync(defaultConfig);

        await db.SaveChangesAsync();
    }
}
