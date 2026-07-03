using Microsoft.EntityFrameworkCore;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(PrmDbContext db)
    {
        if (!await db.Roles.AnyAsync())
        {
            await db.Roles.AddRangeAsync(
                new Role { Name = "Admin" },
                new Role { Name = "Manager" },
                new Role { Name = "Employee" });
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
            await db.Users.AddAsync(new User
            {
                FullName = "System Admin",
                Email = "admin@prm.local",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                RoleId = adminRole.Id,
                IsActive = true,
                ForcePasswordChange = true
            });
        }

        if (!await db.SystemConfigs.AnyAsync())
        {
            await db.SystemConfigs.AddAsync(new SystemConfig
            {
                LlmProvider = "Gemini",
                LlmApiKey = string.Empty,
                SchedulerIntervalHours = 4,
                MaxWeeklyHours = 40
            });
        }

        await SeedEmailTemplatesAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedEmailTemplatesAsync(PrmDbContext db)
    {
        var templates = new[]
        {
            new EmailTemplate
            {
                Name = "Timesheet Reminder 1",
                Subject = "Reminder: Submit your timesheet for week {{WeekStartDate}}",
                Body = "Hello {{EmployeeName}},\n\nYour timesheet for the week starting {{WeekStartDate}} is still pending. The submission deadline was {{SubmissionDeadline}}. Please submit it as soon as possible to avoid restricted timesheet access.\n\nReporting Manager: {{ManagerName}}\n"
            },
            new EmailTemplate
            {
                Name = "Timesheet Reminder 2",
                Subject = "Final reminder: timesheet pending for week {{WeekStartDate}}",
                Body = "Hello {{EmployeeName}},\n\nThis is your second reminder that the timesheet for week {{WeekStartDate}} has not been submitted. If it remains pending, timesheet access will be frozen until your reporting manager restores it.\n\nReporting Manager: {{ManagerName}}\n"
            },
            new EmailTemplate
            {
                Name = "Account Freeze Notification",
                Subject = "Timesheet access frozen for {{EmployeeName}}",
                Body = "Hello,\n\nTimesheet access for {{EmployeeName}} has been frozen because the timesheet for week {{WeekStartDate}} was not submitted after two reminders. The employee can still log in, but cannot create or submit timesheets until access is restored.\n\nReporting Manager: {{ManagerName}}\n"
            },
            new EmailTemplate
            {
                Name = "Account Restore Notification",
                Subject = "Timesheet access restored for {{EmployeeName}}",
                Body = "Hello,\n\nTimesheet access for {{EmployeeName}} has been restored. The employee can now create and submit timesheets again for week {{WeekStartDate}} and later periods.\n\nReporting Manager: {{ManagerName}}\n"
            },
            new EmailTemplate
            {
                Name = "Project At Risk Notification",
                Subject = "Project at risk: {{ProjectName}}",
                Body = "Hello {{ProjectManagerName}},\n\nProject {{ProjectName}} has been marked at risk.\n\nCurrent Health Status: {{CurrentHealthStatus}}\nRisk Level: {{RiskLevel}}\n\nRisk Summary:\n{{RiskSummary}}\n\nKey Milestones:\n{{KeyMilestones}}\n\nSuggested Help:\n{{SuggestedHelp}}\n\nResource Recommendations:\n{{ResourceRecommendations}}\n"
            },
            new EmailTemplate
            {
                Name = "Resource Allocation Notification",
                Subject = "Allocation Notification: You have been allocated to project {{ProjectName}}",
                Body = "Hello {{EmployeeName}},\n\nYou have been allocated to the project '{{ProjectName}}' managed by {{ManagerName}}.\n\nAllocation Details:\n- Utilization: {{UtilisationPercent}}%\n- Start Date: {{FromDate}}\n- End Date: {{ToDate}}\n\nRegards,\nPRM System\n"
            },
            new EmailTemplate
            {
                Name = "Welcome New User",
                Subject = "Welcome to PRM: Your account has been created",
                Body = "Hello {{EmployeeName}},\n\nYour account on the Project Resource Management (PRM) system has been created successfully.\n\nYour Account Details:\n- Username: {{Username}}\n- Email: {{Email}}\n- Temporary Password: {{TemporaryPassword}}\n\nPlease log in and change your password as soon as possible.\n\nRegards,\nPRM System\n"
            },
            new EmailTemplate
            {
                Name = "Password Changed Confirmation",
                Subject = "PRM Account Security Alert: Password Changed",
                Body = "Hello {{EmployeeName}},\n\nThis is a confirmation that the password for your PRM account (username: {{Username}}) has been changed successfully.\n\nIf you did not perform this change, please contact your System Administrator immediately.\n\nRegards,\nPRM System\n"
            },
            new EmailTemplate
            {
                Name = "Manager Assignment Notification",
                Subject = "Reporting Line Updated: Reporting Manager Assigned",
                Body = "Hello {{EmployeeName}},\n\nYou have been assigned to report to manager {{ManagerName}} ({{ManagerEmail}}).\n\nIf you have any questions, please contact your manager or HR.\n\nRegards,\nPRM System\n"
            },
            new EmailTemplate
            {
                Name = "Milestone Upcoming Notification",
                Subject = "PRM Milestone Alert: Milestone {{MilestoneTitle}} is due soon",
                Body = "Hello {{ProjectManagerName}},\n\nThis is a notification that the milestone '{{MilestoneTitle}}' for project '{{ProjectName}}' is due on {{DueDate}}.\n\nMilestone Details:\n- Title: {{MilestoneTitle}}\n- Due Date: {{DueDate}}\n- Story Points: {{StoryPoints}}\n- Status: {{MilestoneStatus}}\n\nPlease ensure task completion or update the status in the system.\n\nRegards,\nPRM System\n"
            }
        };

        var existingNames = await db.EmailTemplates
            .Select(t => t.Name)
            .ToListAsync();

        var missing = templates.Where(t => !existingNames.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Any())
            await db.EmailTemplates.AddRangeAsync(missing);
    }
}
