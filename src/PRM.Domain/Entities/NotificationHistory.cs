using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class NotificationHistory : BaseEntity
{
    public int ProjectId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string SentTo { get; set; } = string.Empty;
    public DateTime SentDate { get; set; }
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    public Project Project { get; set; } = null!;
}