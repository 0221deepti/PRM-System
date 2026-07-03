namespace PRM.Application.DTOs.Notification;

public static class NotificationTemplateNames
{
    public const string TimesheetReminder1 = "Timesheet Reminder 1";
    public const string TimesheetReminder2 = "Timesheet Reminder 2";
    public const string AccountFreezeNotification = "Account Freeze Notification";
    public const string AccountRestoreNotification = "Account Restore Notification";
    public const string ProjectAtRiskNotification = "Project At Risk Notification";
}

public record EmailSendResultDto(
    string RecipientEmail,
    bool IsSuccess,
    string? ErrorMessage);

public record TimesheetAccessStatusDto(
    int EmployeeId,
    DateOnly TrackedWeekStartDate,
    bool IsTimesheetFrozen,
    DateTime? Reminder1SentDate,
    DateTime? Reminder2SentDate,
    DateTime? FreezeDate,
    DateTime? RestoredDate,
    int? RestoredBy);

public record ProjectRiskNotificationContextDto(
    int ProjectId,
    string ProjectName,
    string ProjectManagerName,
    string ProjectManagerEmail,
    string CurrentHealthStatus,
    string RiskLevel,
    string RiskSummary,
    IReadOnlyCollection<string> KeyMilestones,
    IReadOnlyCollection<string> SuggestedHelp,
    IReadOnlyCollection<string> ResourceRecommendations);

public record RemindersProcessResultDto(
    bool Success,
    int EmployeesChecked,
    int Reminders1Sent,
    int Reminders2Sent,
    int AccountsFrozen,
    int AlreadySubmitted,
    string Message,
    DateTime ProcessedAt);