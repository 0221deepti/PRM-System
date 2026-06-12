namespace PRM.Application.DTOs.Allocation;

public record CreateAllocationDto(
    int UserId,
    int ProjectId,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record AllocationSummaryDto(
    int Id,
    int UserId,
    string UserFullName,
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);
