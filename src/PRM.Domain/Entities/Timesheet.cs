namespace PRM.Domain.Entities;

public class Timesheet : BaseEntity
{
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal TotalHoursWorked { get; set; }

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
