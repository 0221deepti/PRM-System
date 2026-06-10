namespace PRM.Domain.Entities;

public class Allocation : BaseEntity
{
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public int UtilisationPercent { get; set; }  // 0–100
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
