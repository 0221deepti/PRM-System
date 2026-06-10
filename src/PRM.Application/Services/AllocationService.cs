using PRM.Application.DTOs.Allocation;
using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class AllocationService : IAllocationService
{
    private readonly IAllocationRepository _allocations;
    private readonly IEmployeeRepository _employees;
    private readonly IProjectRepository _projects;

    public AllocationService(
        IAllocationRepository allocations,
        IEmployeeRepository employees,
        IProjectRepository projects)
    {
        _allocations = allocations;
        _employees = employees;
        _projects = projects;
    }

    public async Task<AllocationSummaryDto> AllocateAsync(CreateAllocationDto dto, int managerEmployeeId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(dto.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.OnHold)
            throw new DomainException("Cannot allocate to a project that is Completed or On Hold.");

        if (dto.FromDate >= dto.ToDate)
            throw new DomainException("FromDate must be before ToDate.");

        if (dto.UtilisationPercent < 1 || dto.UtilisationPercent > 100)
            throw new DomainException("Utilisation percent must be between 1 and 100.");

        var existing = await _allocations.GetTotalUtilisationAsync(
                           dto.EmployeeId, dto.FromDate, dto.ToDate, null, ct);

        if (existing + dto.UtilisationPercent > 100)
            throw new OverAllocationException(
                $"Allocation would bring total to {existing + dto.UtilisationPercent}%. Maximum is 100%.");

        var allocation = new Allocation
        {
            EmployeeId = dto.EmployeeId,
            ProjectId = dto.ProjectId,
            UtilisationPercent = dto.UtilisationPercent,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            IsActive = true
        };

        await _allocations.AddAsync(allocation, ct);
        await _allocations.SaveChangesAsync(ct);

        await UpdateEmployeeStatusAsync(dto.EmployeeId, ct);

        var employee = await _employees.GetWithSkillsAsync(dto.EmployeeId, ct);
        return new AllocationSummaryDto(
            allocation.Id,
            allocation.EmployeeId,
            employee?.User?.FullName ?? "",
            allocation.ProjectId,
            project.Name,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate);
    }

    public async Task EndAllocationAsync(int allocationId, int managerEmployeeId, CancellationToken ct)
    {
        var allocation = await _allocations.GetByIdAsync(allocationId, ct)
                         ?? throw new EntityNotFoundException("Allocation not found.");

        var project = await _projects.GetByIdAsync(allocation.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (project.ManagerId != managerEmployeeId)
            throw new PrmUnauthorizedException("Only the project manager can end this allocation.");

        allocation.ToDate = DateOnly.FromDateTime(DateTime.UtcNow);
        allocation.IsActive = false;
        _allocations.Update(allocation);
        await _allocations.SaveChangesAsync(ct);

        await UpdateEmployeeStatusAsync(allocation.EmployeeId, ct);
    }

    public async Task<IEnumerable<AllocationSummaryDto>> GetAllAllocationsAsync(CancellationToken ct)
    {
        var allocations = await _allocations.GetAllActiveWithDetailsAsync(ct);
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsByProjectAsync(int projectId, CancellationToken ct)
    {
        var allocations = await _allocations.GetActiveByProjectAsync(projectId, ct);
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationSummaryDto>> GetMyAllocationsAsync(int employeeId, CancellationToken ct)
    {
        var allocations = await _allocations.GetActiveByEmployeeAsync(employeeId, ct);
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsForWeekAsync(int employeeId, DateOnly weekStart, CancellationToken ct)
    {
        var allocations = await _allocations.GetActiveByEmployeeAsync(employeeId, ct);
        return allocations
            .Where(a => a.FromDate <= weekStart && a.ToDate >= weekStart)
            .Select(MapToDto);
    }

    private async Task UpdateEmployeeStatusAsync(int employeeId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = await _allocations.GetActiveByEmployeeAsync(employeeId, ct);
        var isCurrentlyAllocated = activeAllocations.Any(a => a.FromDate <= today && a.ToDate >= today);

        var employee = await _employees.GetByIdAsync(employeeId, ct);
        if (employee != null)
        {
            employee.Status = isCurrentlyAllocated ? EmployeeStatus.Allocated : EmployeeStatus.Bench;
            _employees.Update(employee);
            await _employees.SaveChangesAsync(ct);
        }
    }

    private static AllocationSummaryDto MapToDto(Allocation a) =>
        new(a.Id,
            a.EmployeeId,
            a.Employee?.User?.FullName ?? "",
            a.ProjectId,
            a.Project?.Name ?? "",
            a.UtilisationPercent,
            a.FromDate,
            a.ToDate);
}
