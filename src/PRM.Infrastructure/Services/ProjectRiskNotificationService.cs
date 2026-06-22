using PRM.Application.DTOs.Notification;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Infrastructure.Services;

public class ProjectRiskNotificationService : IProjectRiskNotificationService
{
    private readonly INotificationHistoryRepository _notificationHistory;
    private readonly IEmailService _emailService;
    private readonly IRepository<AuditLog> _auditLogs;

    public ProjectRiskNotificationService(
        INotificationHistoryRepository notificationHistory,
        IEmailService emailService,
        IRepository<AuditLog> auditLogs)
    {
        _notificationHistory = notificationHistory;
        _emailService = emailService;
        _auditLogs = auditLogs;
    }

    public async Task NotifyProjectMarkedAtRiskAsync(ProjectRiskNotificationContextDto context, CancellationToken ct)
    {
        if (await _notificationHistory.HasProjectNotificationAsync(
                context.ProjectId,
                NotificationType.ProjectAtRisk,
                context.ProjectManagerEmail,
                ct))
        {
            return;
        }

        var placeholders = new Dictionary<string, string>
        {
            ["ProjectName"] = context.ProjectName,
            ["ProjectManagerName"] = context.ProjectManagerName,
            ["CurrentHealthStatus"] = context.CurrentHealthStatus,
            ["RiskLevel"] = context.RiskLevel,
            ["RiskSummary"] = context.RiskSummary,
            ["KeyMilestones"] = string.Join(Environment.NewLine, context.KeyMilestones),
            ["SuggestedHelp"] = string.Join(Environment.NewLine, context.SuggestedHelp),
            ["ResourceRecommendations"] = string.Join(Environment.NewLine, context.ResourceRecommendations)
        };

        var result = await _emailService.SendTemplateEmailAsync(
            NotificationTemplateNames.ProjectAtRiskNotification,
            context.ProjectManagerEmail,
            placeholders,
            ct);

        await _notificationHistory.AddAsync(new NotificationHistory
        {
            ProjectId = context.ProjectId,
            NotificationType = NotificationType.ProjectAtRisk,
            SentTo = context.ProjectManagerEmail,
            SentDate = DateTime.UtcNow,
            Status = result.IsSuccess ? NotificationDeliveryStatus.Sent : NotificationDeliveryStatus.Failed
        }, ct);

        if (result.IsSuccess)
        {
            await _auditLogs.AddAsync(new AuditLog
            {
                EventType = AuditEventType.ProjectAtRiskNotificationSent,
                ProjectId = context.ProjectId,
                Details = $"Project at-risk notification sent to {context.ProjectManagerEmail}.",
                OccurredAt = DateTime.UtcNow
            }, ct);
        }

        await _notificationHistory.SaveChangesAsync(ct);
    }
}