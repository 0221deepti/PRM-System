using System.ComponentModel.DataAnnotations;

namespace PRM.Application.DTOs.Allocation;

public record CreateAllocationDto(
    [Required(ErrorMessage = "Employee ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Employee ID must be a valid positive integer.")] 
    int UserId,

    [Required(ErrorMessage = "Project ID is required.")] 
    [Range(1, int.MaxValue, ErrorMessage = "Project ID must be a valid positive integer.")] 
    int ProjectId,

    [Required(ErrorMessage = "Utilisation percent is required.")] 
    [Range(1, 100, ErrorMessage = "Utilisation percent must be between 1 and 100.")] 
    int UtilisationPercent,

    [Required(ErrorMessage = "Start date is required.")] 
    DateOnly FromDate,

    [Required(ErrorMessage = "End date is required.")] 
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
