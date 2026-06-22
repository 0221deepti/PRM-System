using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Application.Interfaces.Repositories;

public interface INotificationHistoryRepository : IRepository<NotificationHistory>
{
    Task<bool> HasProjectNotificationAsync(int projectId, NotificationType notificationType, string sentTo, CancellationToken ct = default);
}