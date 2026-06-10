namespace PRM.Domain.Entities;

public class Timesheet : BaseEntity
{
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string ActivityTags { get; set; } = string.Empty;  // comma-separated

    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
