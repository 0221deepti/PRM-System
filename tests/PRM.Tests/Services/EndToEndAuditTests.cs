using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.IO;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PRM.Application.DTOs.Ai;
using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Auth;
using PRM.Application.DTOs.Common;
using PRM.Application.DTOs.Config;
using PRM.Application.DTOs.Employee;
using PRM.Application.DTOs.Project;
using PRM.Application.DTOs.Timesheet;
using PRM.Application.DTOs.User;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Infrastructure.AI;
using PRM.Infrastructure.Persistence;
using Xunit;

namespace PRM.Tests.Services;

public class EndToEndAuditTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly MockSmtpServer _smtpServer;

    public EndToEndAuditTests(WebApplicationFactory<Program> factory)
    {
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "../../../../../"));
        var dbPath = Path.Combine(projectRoot, "prm.db");

        _smtpServer = new MockSmtpServer(2525);
        _smtpServer.Start();

        var mockLlmProvider = new Mock<ILlmProvider>();
        
        // Mock team-builder JSON response
        mockLlmProvider.Setup(p => p.CompleteAsync(
            It.Is<string>(s => s.Contains("Team Builder") || s.Contains("AI-Assisted Team Builder")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
            {
              "recommendations": [
                {
                  "employeeId": 3,
                  "employeeName": "Employee Bob",
                  "department": "Engineering",
                  "skills": "React (Advanced)",
                  "currentUtilisation": 50,
                  "currentStatus": "Allocated",
                  "matchScore": 90,
                  "recommendationReason": "Excellent match."
                }
              ],
              "additionalInsights": "Good matching.",
              "futureExtensibilityNotes": "None."
            }
            """);

        // Mock skill-match JSON response
        mockLlmProvider.Setup(p => p.CompleteAsync(
            It.Is<string>(s => s.Contains("resource allocation assistant")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("[{ \"employeeId\": 3, \"reason\": \"Highly proficient.\" }]");

        // Mock risk-summary response
        mockLlmProvider.Setup(p => p.CompleteAsync(
            It.Is<string>(s => s.Contains("project health analyst")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("The project looks on track with minor risks.");

        var mockAiProviderFactory = new Mock<IAiProviderFactory>();
        mockAiProviderFactory.Setup(f => f.Create(It.IsAny<SystemConfig>()))
            .Returns(mockLlmProvider.Object);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={dbPath}");
            builder.UseSetting("Email:Enabled", "true");
            builder.UseSetting("Email:Host", "127.0.0.1");
            builder.UseSetting("Email:Port", "2525");
            builder.UseSetting("Email:EnableSsl", "false");
            builder.UseSetting("Email:Username", "");
            builder.UseSetting("Email:Password", "");

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAiProviderFactory>(mockAiProviderFactory.Object);
            });
        });
    }

    public void Dispose()
    {
        _smtpServer.Dispose();
    }

    [Fact]
    public async Task Run_Complete_End_To_End_Audit_And_Tests()
    {
        // 1. Database Seeding & Verification
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
            await db.Database.EnsureCreatedAsync();
            await SeedAuditTestDataAsync(db);
        }

        // Initialize Clients
        var client = _factory.CreateClient();

        // 2. Authentication and Authorization Tests
        // 2.1 Valid Login
        var adminToken = await AuthenticateAsync(client, "admin", "Admin@1234");
        adminToken.Should().NotBeNullOrWhiteSpace();

        // Get IDs dynamically
        int employeeRoleId;
        int managerUserId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
            employeeRoleId = (await db.Roles.FirstAsync(r => r.Name == "Employee")).Id;
            managerUserId = (await db.Users.FirstAsync(u => u.Username == "manager")).Id;
        }

        // 2.2 Inactive user login should fail
        Func<Task> loginInactive = async () => await AuthenticateAsync(client, "deactivated_user", "Employee@1234");
        await loginInactive.Should().ThrowAsync<Exception>(); // Returns 401 or 403

        // 3. User Management CRUD
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        // 3.1 Create User (Admin only)
        var createDto = new CreateUserDto(
            "Jane Smith",
            "jane.smith@gmail.com",
            "janesmith",
            "Password@1234",
            employeeRoleId, // Role: Employee
            "Engineering"
        );
        var createUserResponse = await client.PostAsJsonAsync("/api/users", createDto);
        if (createUserResponse.StatusCode != HttpStatusCode.Created)
        {
            var err = await createUserResponse.Content.ReadAsStringAsync();
            throw new Exception($"CreateUser failed: {createUserResponse.StatusCode} - {err}");
        }
        var wrapper = await createUserResponse.Content.ReadFromJsonAsync<ApiResponse<UserSummaryDto>>();
        wrapper.Should().NotBeNull();
        var createdUser = wrapper!.Data;
        createdUser.Should().NotBeNull();
        createdUser!.Username.Should().Be("janesmith");

        // Verify welcome email received
        await Task.Delay(100);
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("Welcome to PRM") && 
            email.Contains("jane.smith@gmail.com") && 
            email.Contains("janesmith"));
        _smtpServer.Clear();

        // DB Verification (Inspect after write)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "janesmith");
            dbUser.Should().NotBeNull();
            dbUser!.Email.Should().Be("jane.smith@gmail.com");
        }

        // 3.1.1 Manager assignment tests (Admin only)
        // Self assignment fails
        var selfAssignDto = new AssignManagerDto(createdUser.Id, createdUser.Id);
        var selfAssignResponse = await client.PutAsJsonAsync($"/api/employees/{createdUser.Id}/assign-manager", selfAssignDto);
        selfAssignResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Assigning non-manager fails (createdUser is employee, has role 3)
        // Let's use the employee user (Id = 3) as the manager
        var badManagerResponse = await client.PutAsJsonAsync($"/api/employees/{createdUser.Id}/assign-manager", new AssignManagerDto(createdUser.Id, 3));
        badManagerResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Valid assignment
        var validAssignDto = new AssignManagerDto(createdUser.Id, managerUserId);
        var validAssignResponse = await client.PutAsJsonAsync($"/api/employees/{createdUser.Id}/assign-manager", validAssignDto);
        validAssignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify manager assignment email received
        await Task.Delay(100);
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("Reporting Line Updated") && 
            email.Contains("jane.smith@gmail.com") && 
            email.Contains("Manager Joe"));
        _smtpServer.Clear();

        // Verify manager is assigned and displayed in employee details
        var employeeDetailResponse = await client.GetAsync($"/api/employees/{createdUser.Id}");
        employeeDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailWrapper = await employeeDetailResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeDetailDto>>();
        detailWrapper.Should().NotBeNull();
        detailWrapper!.Data.ManagerId.Should().Be(managerUserId);
        detailWrapper!.Data.ManagerName.Should().Be("Manager Joe");

        // 3.2 Read User
        var readResponse = await client.GetAsync($"/api/users/{createdUser.Id}");
        if (readResponse.StatusCode != HttpStatusCode.OK)
        {
            var err = await readResponse.Content.ReadAsStringAsync();
            throw new Exception($"ReadUser failed: {readResponse.StatusCode} - {err}");
        }
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3.3 Duplicate Username Rejected (Business Rule)
        var duplicateUserResponse = await client.PostAsJsonAsync("/api/users", createDto with { Email = "diff@gmail.com" });
        duplicateUserResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3.4 Duplicate Email Rejected (Business Rule)
        var duplicateEmailResponse = await client.PostAsJsonAsync("/api/users", createDto with { Username = "diffuser" });
        duplicateEmailResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3.5 Invalid Email Domain Rejected (Business Rule)
        var invalidDomainDto = createDto with { Username = "baddomain", Email = "bad@invalid.domain" };
        var badDomainResponse = await client.PostAsJsonAsync("/api/users", invalidDomainDto);
        badDomainResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 3.6 Deactivate User
        var deactivateResponse = await client.PutAsync($"/api/users/{createdUser.Id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
            var dbUser = await db.Users.FindAsync(createdUser.Id);
            dbUser!.IsActive.Should().BeFalse();
        }

        // 3.7 Reactivate User
        var reactivateResponse = await client.PutAsync($"/api/users/{createdUser.Id}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Employee and Skill Management
        // 4.1 Add Skill to Employee
        var addSkillDto = new AddSkillDto("Kubernetes", SkillCategory.DevOps, SkillProficiency.Intermediate);
        var addSkillResponse = await client.PostAsJsonAsync($"/api/employees/{createdUser.Id}/skills", addSkillDto);
        var addSkillResult = await addSkillResponse.Content.ReadAsStringAsync(); // Ensure response is read if needed
        addSkillResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify duplicate skill assignment is prevented
        var duplicateSkillResponse = await client.PostAsJsonAsync($"/api/employees/{createdUser.Id}/skills", addSkillDto);
        duplicateSkillResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5. Project & Milestone CRUD
        // 5.1 Create Project (Admin)
        var projectDto = new CreateProjectDto(
            "Project Delta",
            "Testing Delta deployment",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31),
            ProjectStatus.Active,
            managerUserId, // ManagerUserId (manager)
            100
        );
        var createProjectResponse = await client.PostAsJsonAsync("/api/projects", projectDto);
        if (createProjectResponse.StatusCode != HttpStatusCode.Created)
        {
            var err = await createProjectResponse.Content.ReadAsStringAsync();
            throw new Exception($"CreateProject failed: {createProjectResponse.StatusCode} - {err}");
        }
        createProjectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var projWrapper = await createProjectResponse.Content.ReadFromJsonAsync<ApiResponse<ProjectSummaryDto>>();
        projWrapper.Should().NotBeNull();
        var projectResult = projWrapper!.Data;

        // 5.2 Validate Project Dates (Business Rule: Start < End)
        var invalidProjectDto = projectDto with { StartDate = new DateOnly(2026, 12, 31), EndDate = new DateOnly(2026, 6, 1) };
        var invalidProjectResponse = await client.PostAsJsonAsync("/api/projects", invalidProjectDto);
        invalidProjectResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5.3 Duplicate Project Name Rejected
        var duplicateProjResponse = await client.PostAsJsonAsync("/api/projects", projectDto);
        duplicateProjResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5.4 Milestone Management
        // 5.4.1 Valid Milestone
        var milestoneDto = new AddMilestoneDto("Milestone 1", new DateOnly(2026, 8, 1), 30);
        var addMilestoneResponse = await client.PostAsJsonAsync($"/api/projects/{projectResult!.Id}/milestones", milestoneDto);
        if (addMilestoneResponse.StatusCode != HttpStatusCode.OK)
        {
            var err = await addMilestoneResponse.Content.ReadAsStringAsync();
            throw new Exception($"AddMilestone failed: {addMilestoneResponse.StatusCode} - {err}");
        }
        addMilestoneResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5.4.2 Milestone Date outside Project Duration Rejected (Business Rule)
        var invalidMilestoneDto = new AddMilestoneDto("Milestone Bad Date", new DateOnly(2027, 8, 1), 30);
        var invalidMilestoneResponse = await client.PostAsJsonAsync($"/api/projects/{projectResult.Id}/milestones", invalidMilestoneDto);
        invalidMilestoneResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 6. Allocation Management
        // Login as Manager to do allocations
        var managerToken = await AuthenticateAsync(client, "manager", "Manager@1234");

        // 6.0 Manager Hierarchy and Project Ownership Authorization Checks
        // 6.0.1 Login as manager2 (Sarah, Id = 5)
        var manager2Token = await AuthenticateAsync(client, "manager2", "Manager@1234");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", manager2Token);

        // 6.0.2 Manager Sarah cannot view detail of employee reporting to Joe
        var unassignedEmployeeDetailResponse = await client.GetAsync($"/api/employees/{createdUser.Id}");
        unassignedEmployeeDetailResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unassignedEmployeeErr = await unassignedEmployeeDetailResponse.Content.ReadFromJsonAsync<ApiResponse>();
        unassignedEmployeeErr!.Message.Should().Be("This employee is not assigned to your team.");

        // 6.0.3 Manager Sarah cannot allocate employee reporting to Joe (Jane Smith)
        var unassignedAllocDto = new CreateAllocationDto(
            createdUser.Id,
            projectResult.Id, // project managed by Joe
            40,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 11, 30)
        );
        var unassignedAllocResponse = await client.PostAsJsonAsync("/api/allocations", unassignedAllocDto);
        unassignedAllocResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unassignedAllocErr = await unassignedAllocResponse.Content.ReadFromJsonAsync<ApiResponse>();
        unassignedAllocErr!.Message.Should().Be("You can only allocate employees who report to you.");

        // 6.0.4 Manager Sarah cannot allocate Bob (who reports to Sarah) to project owned by Joe
        // First Admin assigns Employee Bob to Manager Sarah (Id = 5)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var bobSarahAssignDto = new AssignManagerDto(3, 5);
        var bobSarahAssignResponse = await client.PutAsJsonAsync("/api/employees/3/assign-manager", bobSarahAssignDto);
        bobSarahAssignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", manager2Token);
        var badProjAllocDto = new CreateAllocationDto(
            3, // Bob (reports to Sarah)
            projectResult.Id, // Project Delta (managed by Joe)
            40,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 11, 30)
        );
        var badProjAllocResponse = await client.PostAsJsonAsync("/api/allocations", badProjAllocDto);
        badProjAllocResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badProjAllocErr = await badProjAllocResponse.Content.ReadFromJsonAsync<ApiResponse>();
        badProjAllocErr!.Message.Should().Be("The selected project is not managed by you.");

        // 6.0.5 Manager Sarah has no timesheets submitted for her team yet, should return 400 Bad Request
        var sarahTeamTimesheetsResponse = await client.GetAsync("/api/timesheets/team?weekStart=2026-06-08");
        sarahTeamTimesheetsResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var sarahTeamTimesheetsErr = await sarahTeamTimesheetsResponse.Content.ReadFromJsonAsync<ApiResponse>();
        sarahTeamTimesheetsErr!.Message.Should().Be("No submitted timesheets found for your team.");

        // Re-authenticate as Manager Joe
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        // 6.1 Allocate Employee to Project
        var allocDto = new CreateAllocationDto(
            createdUser.Id, // Employee
            projectResult.Id, // Project Delta
            40, // 40%
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 11, 30)
        );
        var allocResponse = await client.PostAsJsonAsync("/api/allocations", allocDto);
        allocResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var allocWrapper = await allocResponse.Content.ReadFromJsonAsync<ApiResponse<AllocationSummaryDto>>();
        allocWrapper.Should().NotBeNull();
        var allocationResult = allocWrapper!.Data;

        // Verify allocation email received
        await Task.Delay(100);
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("Allocation Notification") && 
            email.Contains("jane.smith@gmail.com") && 
            email.Contains("Project") && 
            email.Contains("Delta"));
        _smtpServer.Clear();

        // 6.2 Over-Allocation Block (> 100% total)
        var overAllocDto = allocDto with { ProjectId = 10, UtilisationPercent = 70 }; // 40 + 70 = 110%
        var overAllocResponse = await client.PostAsJsonAsync("/api/allocations", overAllocDto);
        overAllocResponse.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409 OverAllocationException

        // 6.3 Project Date containment check
        var outOfRangeAllocDto = allocDto with { FromDate = new DateOnly(2026, 5, 1) }; // Project starts 2026-06-01
        var outOfRangeAllocResponse = await client.PostAsJsonAsync("/api/allocations", outOfRangeAllocDto);
        outOfRangeAllocResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 6.4 Duplicate Project Allocation check
        var duplicateAllocResponse = await client.PostAsJsonAsync("/api/allocations", allocDto);
        duplicateAllocResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 7. Timesheet Management
        // Login as Employee to submit timesheet
        var employeeToken = await AuthenticateAsync(client, "employee", "Employee@1234");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        // Note: For submitting, "employee" userId is 3, allocated in seed to project 10 (Aegis Project)
        // 7.1 Valid Timesheet submission
        var timesheetDto = new SubmitTimesheetDto(
            10, // Aegis Project
            new DateOnly(2026, 6, 8), // Monday of week
            8.0m,
            new List<string> { "Development", "Bugfixing" }
        );
        var timesheetResponse = await client.PostAsJsonAsync("/api/timesheets", timesheetDto);
        if (timesheetResponse.StatusCode != HttpStatusCode.OK)
        {
            var err = await timesheetResponse.Content.ReadAsStringAsync();
            throw new Exception($"Timesheet submission failed: {timesheetResponse.StatusCode} - {err}");
        }
        timesheetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7.2 Duplicate submission rejected (Business Rule)
        var duplicateTimesheetResponse = await client.PostAsJsonAsync("/api/timesheets", timesheetDto);
        duplicateTimesheetResponse.StatusCode.Should().Be(HttpStatusCode.Conflict); // 409 DuplicateTimesheetException

        // 7.3 Exceed weekly maximum hours rejected (Business Rule)
        var exceedHoursDto = timesheetDto with { WeekStartDate = new DateOnly(2026, 6, 15), HoursWorked = 50.0m }; // Allocated Max is 50% of 40 = 20
        var exceedHoursResponse = await client.PostAsJsonAsync("/api/timesheets", exceedHoursDto);
        exceedHoursResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 8. AI APIs Validation
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        
        // 8.1 Prompt Empty validation
        var emptyAiQuery = new SkillMatchRequestDto(2, projectResult.Id, "", new DateOnly(2026, 7, 1), new DateOnly(2026, 11, 30), 20);
        var emptyAiResponse = await client.PostAsJsonAsync("/api/ai/skill-match", emptyAiQuery);
        emptyAiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 8.2 Team Builder Query Validation
        var emptyTeamBuilderQuery = new TeamBuilderRequestDto(2, "");
        var emptyTeamBuilderResponse = await client.PostAsJsonAsync("/api/ai/team-builder", emptyTeamBuilderQuery);
        emptyTeamBuilderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 8.3 Valid Skill Match
        var validSkillQuery = new SkillMatchRequestDto(5, projectResult.Id, "I need a senior React frontend developer.", new DateOnly(2026, 7, 1), new DateOnly(2026, 11, 30), 20);
        var skillMatchResponse = await client.PostAsJsonAsync("/api/ai/skill-match", validSkillQuery);
        skillMatchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var skillMatchResult = await skillMatchResponse.Content.ReadFromJsonAsync<ApiResponse<SkillMatchResultDto>>();
        skillMatchResult.Should().NotBeNull();
        skillMatchResult!.Data!.Candidates.Should().NotBeEmpty();
        skillMatchResult.Data.Candidates[0].EmployeeId.Should().Be(3);

        // 8.4 Valid Team Builder
        var validTeamBuilderQuery = new TeamBuilderRequestDto(2, "I need 1 senior frontend React developer.");
        var teamBuilderResponse = await client.PostAsJsonAsync("/api/ai/team-builder", validTeamBuilderQuery);
        teamBuilderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamBuilderResult = await teamBuilderResponse.Content.ReadFromJsonAsync<ApiResponse<TeamBuilderResultDto>>();
        teamBuilderResult.Should().NotBeNull();
        teamBuilderResult!.Data!.Recommendations.Should().NotBeEmpty();
        teamBuilderResult.Data.Recommendations[0].EmployeeId.Should().Be(3);

        // 8.5 Valid Risk Summary
        var validRiskQuery = new RiskSummaryRequestDto(projectResult.Id, 2);
        var riskSummaryResponse = await client.PostAsJsonAsync("/api/ai/risk-summary", validRiskQuery);
        riskSummaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var riskSummaryResult = await riskSummaryResponse.Content.ReadFromJsonAsync<ApiResponse<RiskSummaryDto>>();
        riskSummaryResult.Should().NotBeNull();
        riskSummaryResult!.Data!.Summary.Should().Contain("risks");

        // 9. Email Notification Validation
        // Triggers through EmailService directly inside test
        using (var scope = _factory.Services.CreateScope())
        {
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            // 9.1 Invalid email format format rejected
            var badEmailResult = await emailService.SendTemplateEmailAsync("Timesheet Reminder 1", "bademailformat", new Dictionary<string, string>(), CancellationToken.None);
            badEmailResult.IsSuccess.Should().BeFalse();
            badEmailResult.ErrorMessage.Should().Contain("Invalid email");

            // 9.2 Template not found exception
            Func<Task> badTemplate = async () => await emailService.SendTemplateEmailAsync("NonExistentTemplate", "test@gmail.com", new Dictionary<string, string>(), CancellationToken.None);
            await badTemplate.Should().ThrowAsync<EntityNotFoundException>();
        }

        // 9.3 Change Password email verification
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var changePassDto = new ChangePasswordDto("NewPassword@1234", "NewPassword@1234");
        var changePassResponse = await client.PutAsJsonAsync("/api/auth/change-password", changePassDto);
        changePassResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(100);
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("PRM Account Security Alert: Password Changed") && 
            email.Contains("employee@gmail.com"));
        _smtpServer.Clear();

        // 9.4 Reset Password email verification
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetPassDto = new ResetPasswordDto("AdminReset@1234");
        var resetPassResponse = await client.PutAsJsonAsync($"/api/auth/reset-password/{createdUser.Id}", resetPassDto);
        resetPassResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(100);
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("PRM Account Security Alert: Password Changed") && 
            email.Contains("jane.smith@gmail.com"));
        _smtpServer.Clear();

        // 9.5 Milestone Upcoming notification verification
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
            var proj = await db.Projects.Include(p => p.Milestones).Include(p => p.Manager).FirstAsync(p => p.Id == 10);
            
            var manager = await db.Users.FirstAsync(u => u.Username == "manager");
            proj.ManagerId = manager.Id;
            proj.Manager = manager;
            proj.Status = ProjectStatus.Active;
            db.Projects.Update(proj);
            await db.SaveChangesAsync();

            // Clear existing milestones on project 10
            db.Milestones.RemoveRange(proj.Milestones);
            proj.Milestones.Clear();
            await db.SaveChangesAsync();

            // Add new upcoming milestone
            var ms = new Milestone
            {
                ProjectId = 10,
                Title = "Upcoming Release milestone",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                StoryPoints = 15,
                Status = MilestoneStatus.InProgress
            };
            proj.Milestones.Add(ms);
            await db.SaveChangesAsync();

            var healthService = scope.ServiceProvider.GetRequiredService<IHealthFlaggingService>();
            await healthService.ComputeEmployeeStatusesAsync(CancellationToken.None);
            await healthService.FlagProjectHealthAsync(CancellationToken.None);
        }
        _smtpServer.ReceivedEmails.Should().Contain(email => 
            email.Contains("PRM Milestone Alert") && 
            email.Contains("Upcoming Release milestone") && 
            email.Contains("manager@gmail.com"));
        _smtpServer.Clear();
    }

    public class MockSmtpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<string> _receivedEmails = new();
        private readonly object _lock = new();

        public IReadOnlyList<string> ReceivedEmails
        {
            get
            {
                lock (_lock)
                {
                    return _receivedEmails.ToList();
                }
            }
        }

        public MockSmtpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
        }

        public void Start()
        {
            _listener.Start();
            Task.Run(ListenAsync);
        }

        private async Task ListenAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception) { }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            await writer.WriteLineAsync("220 localhost SMTP ready");

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("HELO") || line.StartsWith("EHLO"))
                {
                    await writer.WriteLineAsync("250-localhost Hello");
                    await writer.WriteLineAsync("250 AUTH LOGIN PLAIN");
                }
                else if (line.StartsWith("MAIL FROM"))
                {
                    await writer.WriteLineAsync("250 2.1.0 Sender OK");
                }
                else if (line.StartsWith("RCPT TO"))
                {
                    await writer.WriteLineAsync("250 2.1.5 Recipient OK");
                }
                else if (line.StartsWith("DATA"))
                {
                    await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                    var data = new System.Text.StringBuilder();
                    string? dataLine;
                    while ((dataLine = await reader.ReadLineAsync()) != null)
                    {
                        if (dataLine == ".")
                            break;
                        data.AppendLine(dataLine);
                    }
                    lock (_lock)
                    {
                        _receivedEmails.Add(data.ToString());
                    }
                    await writer.WriteLineAsync("250 2.0.0 OK : queued");
                }
                else if (line.StartsWith("QUIT"))
                {
                    await writer.WriteLineAsync("221 2.0.0 Service closing transmission channel");
                    break;
                }
                else if (line.StartsWith("AUTH"))
                {
                    await writer.WriteLineAsync("334 VXNlcm5hbWU6");
                    await reader.ReadLineAsync();
                    await writer.WriteLineAsync("334 UGFzc3dvcmQ6");
                    await reader.ReadLineAsync();
                    await writer.WriteLineAsync("235 2.7.0 Authentication successful");
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _receivedEmails.Clear();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }

    private async Task WaitForEmailsAsync(int minCount = 1)
    {
        for (int i = 0; i < 30; i++)
        {
            if (_smtpServer.ReceivedEmails.Count >= minCount)
                return;
            await Task.Delay(100);
        }
    }

    private static async Task<string> AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(username, password));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Authentication failed: {response.StatusCode} - {content}");
        }
        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return wrapper!.Data!.Token;
    }

    private static async Task SeedAuditTestDataAsync(PrmDbContext db)
    {
        // Clean up previously created test data from prior test runs
        var testUser = await db.Users.Include(u => u.Allocations).Include(u => u.Skills).FirstOrDefaultAsync(u => u.Username == "janesmith");
        if (testUser != null)
        {
            db.Allocations.RemoveRange(testUser.Allocations);
            db.UserSkills.RemoveRange(testUser.Skills);
            db.Users.Remove(testUser);
        }
        var manager2 = await db.Users.FirstOrDefaultAsync(u => u.Username == "manager2");
        if (manager2 != null)
        {
            db.Users.Remove(manager2);
        }
        var testProj = await db.Projects.Include(p => p.Allocations).Include(p => p.Milestones).FirstOrDefaultAsync(p => p.Name == "Project Delta");
        if (testProj != null)
        {
            db.Allocations.RemoveRange(testProj.Allocations);
            db.Milestones.RemoveRange(testProj.Milestones);
            db.Projects.Remove(testProj);
        }
        var testTimesheets = await db.Timesheets.Where(t => t.ProjectId == 10 && t.WeekStartDate == new DateOnly(2026, 6, 8)).ToListAsync();
        if (testTimesheets.Any())
        {
            db.Timesheets.RemoveRange(testTimesheets);
        }
        var accessStatuses = await db.Set<EmployeeAccessStatus>().ToListAsync();
        if (accessStatuses.Any())
        {
            db.Set<EmployeeAccessStatus>().RemoveRange(accessStatuses);
        }
        var testAuditLogs = await db.AuditLogs.ToListAsync();
        if (testAuditLogs.Any())
        {
            db.AuditLogs.RemoveRange(testAuditLogs);
        }
        await db.SaveChangesAsync();

        // 1. Roles
        if (!await db.Roles.AnyAsync())
        {
            await db.Roles.AddRangeAsync(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Manager" },
                new Role { Id = 3, Name = "Employee" }
            );
            await db.SaveChangesAsync();
        }

        // 2. Users / Employees
        var adminPass = BCrypt.Net.BCrypt.HashPassword("Admin@1234");
        var managerPass = BCrypt.Net.BCrypt.HashPassword("Manager@1234");
        var employeePass = BCrypt.Net.BCrypt.HashPassword("Employee@1234");

        if (!await db.Users.AnyAsync(u => u.Username == "manager"))
        {
            await db.Users.AddAsync(new User
            {
                Id = 2,
                FullName = "Manager Joe",
                Email = "manager@gmail.com",
                Username = "manager",
                PasswordHash = managerPass,
                RoleId = 2, // Manager
                Department = "Engineering",
                Status = EmployeeStatus.Allocated,
                IsActive = true,
                ForcePasswordChange = false
            });
        }
        else
        {
            var manager = await db.Users.FirstAsync(u => u.Username == "manager");
            manager.ManagerId = null;
            manager.PasswordHash = managerPass;
            manager.ForcePasswordChange = false;
            manager.IsActive = true;
            db.Users.Update(manager);
        }

        if (!await db.Users.AnyAsync(u => u.Username == "employee"))
        {
            await db.Users.AddAsync(new User
            {
                Id = 3,
                FullName = "Employee Bob",
                Email = "employee@gmail.com",
                Username = "employee",
                PasswordHash = employeePass,
                RoleId = 3, // Employee
                Department = "Engineering",
                Status = EmployeeStatus.Allocated,
                IsActive = true,
                ForcePasswordChange = false,
                ManagerId = 2 // Initially reports to Joe
            });
        }
        else
        {
            var employee = await db.Users.FirstAsync(u => u.Username == "employee");
            employee.ManagerId = 2; // Reset manager to Joe
            employee.PasswordHash = employeePass;
            employee.ForcePasswordChange = false;
            employee.IsActive = true;
            db.Users.Update(employee);
        }

        if (!await db.Users.AnyAsync(u => u.Username == "deactivated_user"))
        {
            await db.Users.AddAsync(new User
            {
                Id = 4,
                FullName = "Deactivated User",
                Email = "deactivated@gmail.com",
                Username = "deactivated_user",
                PasswordHash = employeePass,
                RoleId = 3, // Employee
                Department = "QA",
                Status = EmployeeStatus.Bench,
                IsActive = false,
                ForcePasswordChange = false,
                ManagerId = 2
            });
        }
        else
        {
            var deactivated = await db.Users.FirstAsync(u => u.Username == "deactivated_user");
            deactivated.ManagerId = 2; // Reset manager to Joe
            deactivated.PasswordHash = employeePass;
            deactivated.ForcePasswordChange = false;
            deactivated.IsActive = false;
            db.Users.Update(deactivated);
        }

        if (!await db.Users.AnyAsync(u => u.Username == "manager2"))
        {
            await db.Users.AddAsync(new User
            {
                Id = 5,
                FullName = "Manager Sarah",
                Email = "manager2@gmail.com",
                Username = "manager2",
                PasswordHash = managerPass,
                RoleId = 2, // Manager
                Department = "Engineering",
                Status = EmployeeStatus.Allocated,
                IsActive = true,
                ForcePasswordChange = false
            });
        }
        else
        {
            var m2 = await db.Users.FirstAsync(u => u.Username == "manager2");
            m2.PasswordHash = managerPass;
            m2.ForcePasswordChange = false;
            m2.IsActive = true;
            db.Users.Update(m2);
        }

        await db.SaveChangesAsync();

        // 3. Projects
        if (!await db.Projects.AnyAsync(p => p.Id == 10))
        {
            var activeProject = new Project
            {
                Id = 10,
                Name = "Aegis Project",
                Description = "Critical security infrastructure project",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                Status = ProjectStatus.Active,
                ManagerId = 2, // Manager Joe
                TotalStoryPoints = 300,
                HealthStatus = ProjectHealthStatus.OnTrack
            };

            var onHoldProject = new Project
            {
                Id = 11,
                Name = "Demeter Project",
                Description = "Legacy systems migration project",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 8, 30),
                Status = ProjectStatus.OnHold,
                ManagerId = 2, // Manager Joe
                TotalStoryPoints = 150,
                HealthStatus = ProjectHealthStatus.Attention
            };

            await db.Projects.AddRangeAsync(activeProject, onHoldProject);
            await db.SaveChangesAsync();
        }

        // 4. Skills
        if (!await db.Skills.AnyAsync())
        {
            var skill1 = new Skill { Id = 1, Name = "React", Category = SkillCategory.Frontend };
            var skill2 = new Skill { Id = 2, Name = "NodeJS", Category = SkillCategory.Backend };
            var skill3 = new Skill { Id = 3, Name = "SQL", Category = SkillCategory.Backend };
            await db.Skills.AddRangeAsync(skill1, skill2, skill3);
            await db.SaveChangesAsync();
        }

        // 5. UserSkill Map
        if (!await db.UserSkills.AnyAsync())
        {
            await db.UserSkills.AddRangeAsync(
                new UserSkill { UserId = 3, SkillId = 1, Proficiency = SkillProficiency.Advanced },
                new UserSkill { UserId = 3, SkillId = 2, Proficiency = SkillProficiency.Advanced }
            );
            await db.SaveChangesAsync();
        }

        // 6. Allocations
        if (!await db.Allocations.AnyAsync())
        {
            await db.Allocations.AddRangeAsync(
                new Allocation
                {
                    Id = 1,
                    UserId = 3, // Employee Bob
                    ProjectId = 10, // Aegis Project
                    UtilisationPercent = 50, // 50%
                    FromDate = new DateOnly(2026, 1, 1),
                    ToDate = new DateOnly(2026, 12, 31),
                    IsActive = true
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
