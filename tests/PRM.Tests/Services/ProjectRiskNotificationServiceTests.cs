using FluentAssertions;
using Moq;
using PRM.Application.DTOs.Notification;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Infrastructure.Services;

namespace PRM.Tests.Services;

public class ProjectRiskNotificationServiceTests
{
    private readonly Mock<INotificationHistoryRepository> _historyRepoMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IRepository<AuditLog>> _auditLogRepoMock;
    private readonly ProjectRiskNotificationService _service;

    public ProjectRiskNotificationServiceTests()
    {
        _historyRepoMock = new Mock<INotificationHistoryRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _auditLogRepoMock = new Mock<IRepository<AuditLog>>();

        _service = new ProjectRiskNotificationService(
            _historyRepoMock.Object,
            _emailServiceMock.Object,
            _auditLogRepoMock.Object);
    }

    [Fact]
    public async Task NotifyProjectMarkedAtRiskAsync_WhenAlreadySent_DoesNothing()
    {
        var context = new ProjectRiskNotificationContextDto(
            10,
            "Phoenix",
            "Manager One",
            "manager@prm.local",
            "Red",
            "High",
            "Overdue milestones",
            new[] { "M1" },
            new[] { "Replan" },
            new[] { "Alice" });

        _historyRepoMock
            .Setup(r => r.HasProjectNotificationAsync(10, NotificationType.ProjectAtRisk, "manager@prm.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.NotifyProjectMarkedAtRiskAsync(context, CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendTemplateEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _historyRepoMock.Verify(r => r.AddAsync(It.IsAny<NotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyProjectMarkedAtRiskAsync_WhenNewStatus_SendsEmailAndPersistsHistory()
    {
        var context = new ProjectRiskNotificationContextDto(
            10,
            "Phoenix",
            "Manager One",
            "manager@prm.local",
            "Red",
            "High",
            "Overdue milestones",
            new[] { "M1" },
            new[] { "Replan" },
            new[] { "Alice" });

        _historyRepoMock
            .Setup(r => r.HasProjectNotificationAsync(10, NotificationType.ProjectAtRisk, "manager@prm.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _emailServiceMock
            .Setup(s => s.SendTemplateEmailAsync(
                NotificationTemplateNames.ProjectAtRiskNotification,
                "manager@prm.local",
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResultDto("manager@prm.local", true, null));

        await _service.NotifyProjectMarkedAtRiskAsync(context, CancellationToken.None);

        _emailServiceMock.Verify(
            s => s.SendTemplateEmailAsync(
                NotificationTemplateNames.ProjectAtRiskNotification,
                "manager@prm.local",
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _historyRepoMock.Verify(r => r.AddAsync(It.Is<NotificationHistory>(n =>
            n.ProjectId == 10 &&
            n.NotificationType == NotificationType.ProjectAtRisk &&
            n.SentTo == "manager@prm.local" &&
            n.Status == NotificationDeliveryStatus.Sent), It.IsAny<CancellationToken>()), Times.Once);
        _historyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepoMock.Verify(r => r.AddAsync(It.Is<AuditLog>(a =>
            a.EventType == AuditEventType.ProjectAtRiskNotificationSent &&
            a.ProjectId == 10), It.IsAny<CancellationToken>()), Times.Once);
    }
}