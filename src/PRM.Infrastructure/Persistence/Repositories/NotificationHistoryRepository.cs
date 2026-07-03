using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Repositories;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Persistence.Repositories;

public class NotificationHistoryRepository : Repository<NotificationHistory>, INotificationHistoryRepository
{
    public NotificationHistoryRepository(PrmDbContext db) : base(db) { }

    public async Task<bool> HasProjectNotificationAsync(int projectId, NotificationType notificationType, string sentTo, CancellationToken ct = default)
        => await _set.AnyAsync(
            x => x.ProjectId == projectId
                 && x.NotificationType == notificationType
                 && x.SentTo == sentTo
                 && x.Status == NotificationDeliveryStatus.Sent,
            ct);
}