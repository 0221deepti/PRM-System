using PRM.Application.DTOs.Notification;

namespace PRM.Application.Interfaces.Services;

public interface IProjectRiskNotificationService
{
    Task NotifyProjectMarkedAtRiskAsync(ProjectRiskNotificationContextDto context, CancellationToken ct);
}