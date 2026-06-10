namespace PRM.Application.DTOs.Allocation;

public record CreateAllocationDto(
    int EmployeeId,
    int ProjectId,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);
