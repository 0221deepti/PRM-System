using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class AuditLog : BaseEntity
{
    public AuditEventType EventType { get; set; }
    public int? EmployeeId { get; set; }
    public int? ProjectId { get; set; }
    public int? PerformedByUserId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public User? Employee { get; set; }
    public Project? Project { get; set; }
    public User? PerformedByUser { get; set; }
}