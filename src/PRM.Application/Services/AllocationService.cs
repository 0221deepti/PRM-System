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
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;

    public AllocationService(
        IAllocationRepository allocations,
        IUserRepository users,
        IProjectRepository projects)
    {
        _allocations = allocations;
        _users = users;
        _projects = projects;
    }

    public async Task<AllocationSummaryDto> AllocateAsync(CreateAllocationDto dto, int managerUserId, CancellationToken ct)
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
                           dto.UserId, dto.FromDate, dto.ToDate, null, ct);

        if (existing + dto.UtilisationPercent > 100)
            throw new OverAllocationException(
                $"Allocation would bring total to {existing + dto.UtilisationPercent}%. Maximum is 100%.");

        var allocation = new Allocation
        {
            UserId = dto.UserId,
            ProjectId = dto.ProjectId,
            UtilisationPercent = dto.UtilisationPercent,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            IsActive = true
        };

        await _allocations.AddAsync(allocation, ct);
        await _allocations.SaveChangesAsync(ct);

        await UpdateUserStatusAsync(dto.UserId, ct);

        var user = await _users.GetWithSkillsAsync(dto.UserId, ct);
        return new AllocationSummaryDto(
            allocation.Id,
            allocation.UserId,
            user?.FullName ?? "",
            allocation.ProjectId,
            project.Name,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate);
    }

    public async Task EndAllocationAsync(int allocationId, int managerUserId, CancellationToken ct)
    {
        var allocation = await _allocations.GetByIdAsync(allocationId, ct)
                         ?? throw new EntityNotFoundException("Allocation not found.");

        var project = await _projects.GetByIdAsync(allocation.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (project.ManagerId != managerUserId)
            throw new PrmUnauthorizedException("Only the project manager can end this allocation.");

        allocation.ToDate = DateOnly.FromDateTime(DateTime.UtcNow);
        allocation.IsActive = false;
        _allocations.Update(allocation);
        await _allocations.SaveChangesAsync(ct);

        await UpdateUserStatusAsync(allocation.UserId, ct);
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

    public async Task<IEnumerable<AllocationSummaryDto>> GetMyAllocationsAsync(int userId, CancellationToken ct)
    {
        var allocations = await _allocations.GetActiveByUserAsync(userId, ct);
        return allocations.Select(MapToDto);
    }

    public async Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsForWeekAsync(int userId, DateOnly weekStart, CancellationToken ct)
    {
        var allocations = await _allocations.GetActiveByUserAsync(userId, ct);
        return allocations
            .Where(a => a.FromDate <= weekStart && a.ToDate >= weekStart)
            .Select(MapToDto);
    }

    private async Task UpdateUserStatusAsync(int userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = await _allocations.GetActiveByUserAsync(userId, ct);
        var isCurrentlyAllocated = activeAllocations.Any(a => a.FromDate <= today && a.ToDate >= today);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user != null)
        {
            user.Status = isCurrentlyAllocated ? EmployeeStatus.Allocated : EmployeeStatus.Bench;
            _users.Update(user);
            await _users.SaveChangesAsync(ct);
        }
    }

    private static AllocationSummaryDto MapToDto(Allocation a) =>
        new(a.Id,
            a.UserId,
            a.User?.FullName ?? "",
            a.ProjectId,
            a.Project?.Name ?? "",
            a.UtilisationPercent,
            a.FromDate,
            a.ToDate);
}
