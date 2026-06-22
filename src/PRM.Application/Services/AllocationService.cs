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
    private readonly IEmailService _emailService;

    public AllocationService(
        IAllocationRepository allocations,
        IUserRepository users,
        IProjectRepository projects,
        IEmailService emailService)
    {
        _allocations = allocations;
        _users = users;
        _projects = projects;
        _emailService = emailService;
    }

    public async Task<(AllocationSummaryDto Allocation, string? WarningMessage)> AllocateAsync(CreateAllocationDto dto, int managerUserId, CancellationToken ct)
    {
        var employee = await _users.GetByIdAsync(dto.UserId, ct)
                       ?? throw new EntityNotFoundException("Employee not found.");

        if (employee.ManagerId != managerUserId)
            throw new DomainException("You can only allocate employees who report to you.");

        if (!employee.IsActive)
            throw new DomainException("Cannot allocate resources to an inactive employee.");

        var project = await _projects.GetByIdAsync(dto.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (project.ManagerId != managerUserId)
            throw new DomainException("The selected project is not managed by you.");

        if (project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.OnHold)
            throw new DomainException("Cannot allocate to a project that is Completed or On Hold.");

        if (dto.FromDate >= dto.ToDate)
            throw new DomainException("FromDate must be before ToDate.");

        if (dto.FromDate < project.StartDate || dto.ToDate > project.EndDate)
            throw new DomainException($"Allocation dates ({dto.FromDate} to {dto.ToDate}) must fall within the project duration ({project.StartDate} to {project.EndDate}).");

        if (dto.UtilisationPercent < 1 || dto.UtilisationPercent > 100)
            throw new DomainException("Utilisation percent must be between 1 and 100.");

        // Prevent duplicate project allocation in overlapping periods
        var activeAllocs = await _allocations.GetActiveByUserAsync(dto.UserId, ct);
        var isAlreadyAllocated = activeAllocs.Any(a => 
            a.ProjectId == dto.ProjectId && 
            a.FromDate <= dto.ToDate && 
            a.ToDate >= dto.FromDate);
        if (isAlreadyAllocated)
            throw new DomainException("Employee is already allocated to this project during the specified period.");

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
        var summary = new AllocationSummaryDto(
            allocation.Id,
            allocation.UserId,
            user?.FullName ?? "",
            allocation.ProjectId,
            project.Name,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate);

        string? warningMessage = null;
        try
        {
            var manager = await _users.GetByIdAsync(managerUserId, ct);
            var placeholders = new Dictionary<string, string>
            {
                ["EmployeeName"] = user?.FullName ?? employee.FullName,
                ["ProjectName"] = project.Name,
                ["ManagerName"] = manager?.FullName ?? "Reporting Manager",
                ["UtilisationPercent"] = allocation.UtilisationPercent.ToString(),
                ["FromDate"] = allocation.FromDate.ToString("yyyy-MM-dd"),
                ["ToDate"] = allocation.ToDate.ToString("yyyy-MM-dd")
            };
            var emailResult = await _emailService.SendTemplateEmailAsync("Resource Allocation Notification", employee.Email, placeholders, ct);
            if (!emailResult.IsSuccess)
            {
                warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
            }
        }
        catch (Exception)
        {
            warningMessage = "Unable to send notification email. The requested operation completed successfully, but email delivery failed.";
        }

        return (summary, warningMessage);
    }

    public async Task EndAllocationAsync(int allocationId, int managerUserId, CancellationToken ct)
    {
        var allocation = await _allocations.GetByIdAsync(allocationId, ct)
                         ?? throw new EntityNotFoundException("Allocation not found.");

        var project = await _projects.GetByIdAsync(allocation.ProjectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (project.ManagerId != managerUserId)
            throw new DomainException("The selected project is not managed by you.");

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

    public async Task<IEnumerable<AllocationSummaryDto>> GetActiveAllocationsByProjectAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (callerRole == "Manager" && project.ManagerId != callerUserId)
            throw new DomainException("The selected project is not managed by you.");

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
