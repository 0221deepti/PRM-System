using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public int ManagerId { get; set; }  // FK → User
    public int TotalStoryPoints { get; set; }
    public ProjectHealthStatus HealthStatus { get; set; } = ProjectHealthStatus.OnTrack;

    // Navigation
    public User Manager { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<NotificationHistory> NotificationHistories { get; set; } = new List<NotificationHistory>();
}
