using FluentAssertions;
using Moq;
using PRM.Application.DTOs.Timesheet;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Services;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Tests.Services;

public class TimesheetServiceTests
{
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<ISystemConfigRepository> _configRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRepository<ActivityTag>> _tagRepoMock;
    private readonly Mock<IRepository<TimesheetEntry>> _entryRepoMock;
    private readonly Mock<IRepository<TimesheetEntryTag>> _entryTagRepoMock;
    private readonly TimesheetService _service;

    public TimesheetServiceTests()
    {
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _configRepoMock = new Mock<ISystemConfigRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _tagRepoMock = new Mock<IRepository<ActivityTag>>();
        _entryRepoMock = new Mock<IRepository<TimesheetEntry>>();
        _entryTagRepoMock = new Mock<IRepository<TimesheetEntryTag>>();

        _service = new TimesheetService(
            _timesheetRepoMock.Object,
            _allocationRepoMock.Object,
            _configRepoMock.Object,
            _userRepoMock.Object,
            _tagRepoMock.Object,
            _entryRepoMock.Object,
            _entryTagRepoMock.Object);
    }

    [Fact]
    public async Task SubmitAsync_FutureDate_ThrowsDomainException()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var dto = new SubmitTimesheetDto(10, futureDate, 40, new List<string> { "Dev" });

        _configRepoMock.Setup(c => c.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfig { MaxWeeklyHours = 40 });

        // Act
        Func<Task> action = async () => await _service.SubmitAsync(dto, 1, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot submit a timesheet for a future week.");
    }

    [Fact]
    public async Task SubmitAsync_DuplicateTimesheet_ThrowsDuplicateTimesheetException()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = new SubmitTimesheetDto(10, today, 40, new List<string> { "Dev" });

        _configRepoMock.Setup(c => c.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfig { MaxWeeklyHours = 40 });
        _timesheetRepoMock.Setup(t => t.ExistsAsync(1, 10, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> action = async () => await _service.SubmitAsync(dto, 1, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DuplicateTimesheetException>()
            .WithMessage("A timesheet for this project and week already exists.");
    }

    [Fact]
    public async Task SubmitAsync_ExceedsAllocation_ThrowsDomainException()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = new SubmitTimesheetDto(10, today, 30, new List<string> { "Dev" }); // logging 30 hours

        _configRepoMock.Setup(c => c.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfig { MaxWeeklyHours = 40 });
        _timesheetRepoMock.Setup(t => t.ExistsAsync(1, 10, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Employee allocated for 50% = 20 hours max
        var allocations = new List<Allocation>
        {
            new() { ProjectId = 10, UtilisationPercent = 50, FromDate = today.AddDays(-1), ToDate = today.AddDays(10) }
        };
        _allocationRepoMock.Setup(a => a.GetActiveByUserAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        // Act
        Func<Task> action = async () => await _service.SubmitAsync(dto, 1, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Hours logged (30) exceed the allowed maximum for this allocation (20.0 hrs).");
    }
}
