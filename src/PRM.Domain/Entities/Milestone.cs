using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class Milestone : BaseEntity
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    public Project Project { get; set; } = null!;
}
