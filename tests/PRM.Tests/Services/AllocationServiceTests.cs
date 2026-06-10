using FluentAssertions;
using Moq;
using PRM.Application.DTOs.Allocation;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Tests.Services;

public class AllocationServiceTests
{
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly AllocationService _service;

    public AllocationServiceTests()
    {
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();

        _service = new AllocationService(
            _allocationRepoMock.Object,
            _employeeRepoMock.Object,
            _projectRepoMock.Object);
    }

    [Fact]
    public async Task AllocateAsync_TotalExceeds100Percent_ThrowsOverAllocationException()
    {
        // Arrange
        var employee = new Employee { Id = 1, Status = EmployeeStatus.Bench };
        var project = new Project { Id = 10, Status = ProjectStatus.Active };
        var dto = new CreateAllocationDto(1, 10, 60, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _projectRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        _allocationRepoMock.Setup(r => r.GetTotalUtilisationAsync(1, dto.FromDate, dto.ToDate, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        // Act
        Func<Task> action = async () => await _service.AllocateAsync(dto, 999, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<OverAllocationException>()
            .WithMessage("Allocation would bring total to 110%. Maximum is 100%.");
    }

    [Fact]
    public async Task AllocateAsync_ValidRequest_CreatesAllocation()
    {
        // Arrange
        var employee = new Employee { Id = 1, Status = EmployeeStatus.Bench };
        var project = new Project { Id = 10, Status = ProjectStatus.Active };
        var dto = new CreateAllocationDto(1, 10, 50, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _projectRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation>());

        // Act
        var result = await _service.AllocateAsync(dto, 999, CancellationToken.None);

        // Assert
        _allocationRepoMock.Verify(r => r.AddAsync(It.IsAny<Allocation>(), It.IsAny<CancellationToken>()), Times.Once);
        _allocationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
        result.UtilisationPercent.Should().Be(50);
    }
}
