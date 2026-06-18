namespace PRM.Domain.Entities;

public class TimesheetEntry : BaseEntity
{
    public int TimesheetId { get; set; }
    public DateOnly EntryDate { get; set; }
    public decimal HoursWorked { get; set; }
    public string Description { get; set; } = string.Empty;

    public Timesheet Timesheet { get; set; } = null!;
    public ICollection<TimesheetEntryTag> Tags { get; set; } = new List<TimesheetEntryTag>();
}
