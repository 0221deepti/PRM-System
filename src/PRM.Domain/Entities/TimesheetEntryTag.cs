namespace PRM.Domain.Entities;

public class TimesheetEntryTag : BaseEntity
{
    public int TimesheetEntryId { get; set; }
    public int ActivityTagId { get; set; }

    public TimesheetEntry TimesheetEntry { get; set; } = null!;
    public ActivityTag ActivityTag { get; set; } = null!;
}
