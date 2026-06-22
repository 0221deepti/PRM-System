using PRM.Application.DTOs.Project;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;

    public ProjectService(IProjectRepository projects, IEmployeeRepository employees)
    {
        _projects = projects;
        _employees = employees;
    }

    public async Task<ProjectSummaryDto> CreateProjectAsync(CreateProjectDto dto, CancellationToken ct)
    {
        var trimmedName = dto.Name.Trim();
        var trimmedDesc = dto.Description?.Trim() ?? string.Empty;

        if (dto.StartDate >= dto.EndDate)
            throw new DomainException("Start date must be before end date.");

        // Manager check
        var manager = await _employees.GetByIdAsync(dto.ManagerId, ct)
                      ?? throw new EntityNotFoundException("Manager employee not found.");

        // Unique Name check
        var allProjects = await _projects.GetAllAsync(ct);
        if (allProjects.Any(p => p.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"A project with the name '{trimmedName}' already exists.");

        var project = new Project
        {
            Name = trimmedName,
            Description = trimmedDesc,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            ManagerId = dto.ManagerId,
            TotalStoryPoints = dto.TotalStoryPoints,
            HealthStatus = ProjectHealthStatus.OnTrack
        };

        await _projects.AddAsync(project, ct);
        await _projects.SaveChangesAsync(ct);

        return new ProjectSummaryDto(
            project.Id, project.Name,
            manager.FullName ?? "",
            project.EndDate, project.Status,
            project.HealthStatus, 0,
            project.TotalStoryPoints);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetAllProjectsAsync(CancellationToken ct)
    {
        var projects = await _projects.GetAllWithDetailsAsync(ct);
        return projects.Select(MapToSummary);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetManagerProjectsAsync(int managerEmployeeId, CancellationToken ct)
    {
        var projects = await _projects.GetByManagerAsync(managerEmployeeId, ct);
        return projects.Select(MapToSummary);
    }

    public async Task<ProjectDetailDto?> GetProjectDetailAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct)
    {
        var project = await _projects.GetWithAllocationsAsync(projectId, ct);
        if (project == null) return null;

        if (callerRole == "Manager" && project.ManagerId != callerUserId)
            throw new DomainException("The selected project is not managed by you.");

        if (callerRole == "Employee" && !project.Allocations.Any(a => a.UserId == callerUserId && a.IsActive))
            throw new PrmUnauthorizedException("You are not allocated to this project.");

        return MapToDetail(project);
    }

    public async Task UpdateProjectAsync(int projectId, UpdateProjectDto dto, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        var trimmedName = dto.Name.Trim();
        var trimmedDesc = dto.Description?.Trim() ?? string.Empty;

        if (dto.StartDate >= dto.EndDate)
            throw new DomainException("Start date must be before end date.");

        // Manager check
        _ = await _employees.GetByIdAsync(dto.ManagerId, ct)
            ?? throw new EntityNotFoundException("Manager employee not found.");

        // Unique Name check (excluding current project)
        var allProjects = await _projects.GetAllAsync(ct);
        if (allProjects.Any(p => p.Id != projectId && p.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"A project with the name '{trimmedName}' already exists.");

        project.Name = trimmedName;
        project.Description = trimmedDesc;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.Status = dto.Status;
        project.ManagerId = dto.ManagerId;
        project.TotalStoryPoints = dto.TotalStoryPoints;

        _projects.Update(project);
        await _projects.SaveChangesAsync(ct);
    }

    private static ProjectSummaryDto MapToSummary(Project p)
    {
        var donePoints = p.Milestones.Where(m => m.Status == MilestoneStatus.Done).Sum(m => m.StoryPoints);
        return new ProjectSummaryDto(
            p.Id, p.Name,
            p.Manager?.FullName ?? "",
            p.EndDate, p.Status, p.HealthStatus,
            donePoints, p.TotalStoryPoints);
    }

    private static ProjectDetailDto MapToDetail(Project p)
    {
        var milestones = p.Milestones.Select(m =>
            new MilestoneSummaryDto(m.Id, m.Title, m.DueDate, m.StoryPoints, m.Status)).ToList();

        var allocations = p.Allocations.Where(a => a.IsActive).Select(a =>
            new ProjectAllocationSummaryDto(
                a.Id, a.UserId,
                a.User?.FullName ?? "",
                a.ProjectId, p.Name,
                a.UtilisationPercent, a.FromDate, a.ToDate)).ToList();

        return new ProjectDetailDto(
            p.Id, p.Name, p.Description,
            p.StartDate, p.EndDate, p.Status, p.HealthStatus,
            p.ManagerId, p.Manager?.FullName ?? "",
            p.TotalStoryPoints, milestones, allocations);
    }
}

public class MilestoneService : IMilestoneService
{
    private readonly IProjectRepository _projects;

    public MilestoneService(IProjectRepository projects) => _projects = projects;

    public async Task<MilestoneSummaryDto> AddMilestoneAsync(int projectId, AddMilestoneDto dto, int callerUserId, string callerRole, CancellationToken ct)
    {
        var project = await _projects.GetWithMilestonesAsync(projectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (callerRole == "Manager" && project.ManagerId != callerUserId)
            throw new DomainException("The selected project is not managed by you.");

        if (dto.DueDate < project.StartDate || dto.DueDate > project.EndDate)
            throw new DomainException($"Milestone Due Date ({dto.DueDate}) must fall within the project duration ({project.StartDate} to {project.EndDate}).");

        var milestone = new Milestone
        {
            ProjectId = projectId,
            Title = dto.Title.Trim(),
            DueDate = dto.DueDate,
            StoryPoints = dto.StoryPoints,
            Status = MilestoneStatus.NotStarted
        };

        project.Milestones.Add(milestone);
        _projects.Update(project);
        await _projects.SaveChangesAsync(ct);

        return new MilestoneSummaryDto(milestone.Id, milestone.Title, milestone.DueDate, milestone.StoryPoints, milestone.Status);
    }

    public async Task UpdateMilestoneStatusAsync(int milestoneId, UpdateMilestoneStatusDto dto, int callerUserId, string callerRole, CancellationToken ct)
    {
        var projects = await _projects.GetAllWithDetailsAsync(ct);
        var project = projects.FirstOrDefault(p => p.Milestones.Any(m => m.Id == milestoneId))
                      ?? throw new EntityNotFoundException("Milestone not found.");

        if (callerRole == "Manager" && project.ManagerId != callerUserId)
            throw new DomainException("The selected project is not managed by you.");

        var milestone = project.Milestones.First(m => m.Id == milestoneId);
        milestone.Status = dto.Status;
        _projects.Update(project);
        await _projects.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<MilestoneSummaryDto>> GetProjectMilestonesAsync(int projectId, int callerUserId, string callerRole, CancellationToken ct)
    {
        var project = await _projects.GetWithAllocationsAsync(projectId, ct)
                      ?? throw new EntityNotFoundException("Project not found.");

        if (callerRole == "Manager" && project.ManagerId != callerUserId)
            throw new DomainException("The selected project is not managed by you.");

        if (callerRole == "Employee" && !project.Allocations.Any(a => a.UserId == callerUserId && a.IsActive))
            throw new PrmUnauthorizedException("You are not allocated to this project.");

        return project.Milestones.Select(m =>
            new MilestoneSummaryDto(m.Id, m.Title, m.DueDate, m.StoryPoints, m.Status));
    }
}
