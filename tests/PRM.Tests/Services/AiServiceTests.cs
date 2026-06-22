using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using PRM.Application.DTOs.Ai;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Infrastructure.AI;
using PRM.Infrastructure.Services;
using System.Text.Json;

namespace PRM.Tests.Services;

public class AiServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<ISystemConfigRepository> _configRepoMock;
    private readonly Mock<IAiProviderFactory> _providerFactoryMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<ILogger<AiService>> _loggerMock;
    private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _configurationMock;
    private readonly AiService _service;

    public AiServiceTests()
    {
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _configRepoMock = new Mock<ISystemConfigRepository>();
        _providerFactoryMock = new Mock<IAiProviderFactory>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _loggerMock = new Mock<ILogger<AiService>>();
        _configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();

        _service = new AiService(
            _employeeRepoMock.Object,
            _projectRepoMock.Object,
            _timesheetRepoMock.Object,
            _allocationRepoMock.Object,
            _configRepoMock.Object,
            _providerFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task BuildTeamAsync_ValidRequirement_ReturnsExpectedRecommendations()
    {
        // Arrange
        var employeeRole = new Role { Name = "Employee" };
        var skillReact = new Skill { Name = "React" };

        var userA = new User
        {
            Id = 101,
            FullName = "User A",
            Department = "Engineering",
            Status = EmployeeStatus.Bench,
            IsActive = true,
            Role = employeeRole,
            Skills = new List<UserSkill> { new UserSkill { Skill = skillReact, Proficiency = SkillProficiency.Advanced } },
            Allocations = new List<Allocation>()
        };

        var userB = new User
        {
            Id = 102,
            FullName = "User B",
            Department = "Engineering",
            Status = EmployeeStatus.Allocated,
            IsActive = true,
            Role = employeeRole,
            Skills = new List<UserSkill> { new UserSkill { Skill = skillReact, Proficiency = SkillProficiency.Intermediate } },
            Allocations = new List<Allocation> { new Allocation { IsActive = true, UtilisationPercent = 20 } }
        };

        var usersList = new List<User> { userA, userB };

        _employeeRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        var config = new SystemConfig { LlmProvider = "LocalGemma" };
        _configRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var fakeJsonResponse = @"
        {
          ""recommendations"": [
            {
              ""employeeId"": 101,
              ""employeeName"": ""User A"",
              ""department"": ""Engineering"",
              ""skills"": ""React (Advanced)"",
              ""currentUtilisation"": 0,
              ""currentStatus"": ""Bench"",
              ""matchScore"": 95,
              ""recommendationReason"": ""Bench resource and skilled in React.""
            },
            {
              ""employeeId"": 102,
              ""employeeName"": ""User B"",
              ""department"": ""Engineering"",
              ""skills"": ""React (Intermediate)"",
              ""currentUtilisation"": 20,
              ""currentStatus"": ""Allocated"",
              ""matchScore"": 80,
              ""recommendationReason"": ""Low utilization of 20%.""
            }
          ],
          ""additionalInsights"": ""2 requested resources were found."",
          ""futureExtensibilityNotes"": ""Shortage predicted: None.""
        }";

        _llmProviderMock.Setup(p => p.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeJsonResponse);

        _providerFactoryMock.Setup(f => f.Create(config))
            .Returns(_llmProviderMock.Object);

        var request = new TeamBuilderRequestDto(1, "I need 2 React developers.");

        // Act
        var result = await _service.BuildTeamAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Recommendations.Should().HaveCount(2);
        result.Recommendations[0].EmployeeId.Should().Be(101);
        result.Recommendations[0].EmployeeName.Should().Be("User A");
        result.Recommendations[0].CurrentStatus.Should().Be("Bench");
        result.Recommendations[0].CurrentUtilisation.Should().Be(0);
        result.Recommendations[0].MatchScore.Should().Be(95);

        result.Recommendations[1].EmployeeId.Should().Be(102);
        result.Recommendations[1].EmployeeName.Should().Be("User B");
        result.Recommendations[1].CurrentStatus.Should().Be("Allocated");
        result.Recommendations[1].CurrentUtilisation.Should().Be(20);
        result.Recommendations[1].MatchScore.Should().Be(80);

        result.AdditionalInsights.Should().Be("2 requested resources were found.");
        result.FutureExtensibilityNotes.Should().Be("Shortage predicted: None.");
    }

    [Fact]
    public async Task BuildTeamAsync_RequirementTooShort_ThrowsDomainException()
    {
        // Arrange
        var request = new TeamBuilderRequestDto(1, "abc"); // Less than 5 characters

        // Act
        Func<Task> act = async () => await _service.BuildTeamAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Natural language requirement must be at least 5 characters.");
    }

    [Fact]
    public async Task BuildTeamAsync_TimeoutException_ThrowsFriendlyException()
    {
        // Arrange
        var request = new TeamBuilderRequestDto(1, "I need some developer resources for my React project.");
        var config = new SystemConfig { LlmProvider = "LocalGemma" };
        
        _employeeRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new User { Id = 101, IsActive = true, Role = new Role { Name = "Employee" } } });
        _configRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _providerFactoryMock.Setup(f => f.Create(config))
            .Returns(_llmProviderMock.Object);
        _llmProviderMock.Setup(p => p.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task> act = async () => await _service.BuildTeamAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("AI service is currently unavailable. Please try again later.");
    }

    [Fact]
    public async Task BuildTeamAsync_ProviderException_ThrowsFriendlyException()
    {
        // Arrange
        var request = new TeamBuilderRequestDto(1, "I need some developer resources for my React project.");
        var config = new SystemConfig { LlmProvider = "LocalGemma" };
        
        _employeeRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new User { Id = 101, IsActive = true, Role = new Role { Name = "Employee" } } });
        _configRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _providerFactoryMock.Setup(f => f.Create(config))
            .Returns(_llmProviderMock.Object);
        _llmProviderMock.Setup(p => p.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Ollama server down"));

        // Act
        Func<Task> act = async () => await _service.BuildTeamAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("AI service is currently unavailable. Please try again later.");
    }

    [Fact]
    public async Task BuildTeamAsync_InvalidJsonResponse_ThrowsFriendlyException()
    {
        // Arrange
        var request = new TeamBuilderRequestDto(1, "I need some developer resources for my React project.");
        var config = new SystemConfig { LlmProvider = "LocalGemma" };
        
        _employeeRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new User { Id = 101, IsActive = true, Role = new Role { Name = "Employee" } } });
        _configRepoMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _providerFactoryMock.Setup(f => f.Create(config))
            .Returns(_llmProviderMock.Object);
        _llmProviderMock.Setup(p => p.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Not JSON response at all");

        // Act
        Func<Task> act = async () => await _service.BuildTeamAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("AI service is currently unavailable. Please try again later.");
    }
}
