using FluentAssertions;
using Moq;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();

        _service = new EmployeeService(_employeeRepoMock.Object, _userRepoMock.Object, _allocationRepoMock.Object);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_ValidEmployee_DeactivatesUserAndEndsAllocations()
    {
        // Arrange
        var allocation = new Allocation { Id = 10, IsActive = true, ToDate = new DateOnly(2026, 12, 31) };
        var employee = new User 
        { 
            Id = 5, 
            IsActive = true,
            Status = EmployeeStatus.Allocated,
            Allocations = new List<Allocation> { allocation }
        };

        _employeeRepoMock.Setup(e => e.GetWithAllocationsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _userRepoMock.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        await _service.DeactivateEmployeeAsync(5, CancellationToken.None);

        // Assert
        employee.Status.Should().Be(EmployeeStatus.Bench);
        employee.IsActive.Should().BeFalse();
        allocation.IsActive.Should().BeFalse();
        allocation.ToDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        _userRepoMock.Verify(u => u.Update(It.IsAny<User>()), Times.Once);
        _employeeRepoMock.Verify(e => e.Update(It.IsAny<User>()), Times.Once);
        _allocationsVerify(allocation);
        _employeeRepoMock.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void _allocationsVerify(Allocation a)
    {
        _allocationRepoMock.Verify(r => r.Update(a), Times.Once);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        _employeeRepoMock.Setup(e => e.GetWithAllocationsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> action = async () => await _service.DeactivateEmployeeAsync(99, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("Employee not found.");
    }
}
