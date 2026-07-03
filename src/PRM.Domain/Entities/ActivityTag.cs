namespace PRM.Domain.Entities;

public class ActivityTag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";  // Optional: for UI

    public ICollection<TimesheetEntryTag> EntryTags { get; set; } = new List<TimesheetEntryTag>();
}
