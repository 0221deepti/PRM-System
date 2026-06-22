namespace PRM.Domain.Entities;

public class EmployeeAccessStatus : BaseEntity
{
    public int EmployeeId { get; set; }
    public DateOnly TrackedWeekStartDate { get; set; }
    public bool IsTimesheetFrozen { get; set; }
    public DateTime? Reminder1SentDate { get; set; }
    public DateTime? Reminder2SentDate { get; set; }
    public DateTime? FreezeDate { get; set; }
    public DateTime? RestoredDate { get; set; }
    public int? RestoredBy { get; set; }

    public User Employee { get; set; } = null!;
    public User? RestoredByUser { get; set; }
}