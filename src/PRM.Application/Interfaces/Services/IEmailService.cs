using PRM.Application.DTOs.Notification;

namespace PRM.Application.Interfaces.Services;

public interface IEmailService
{
    Task<EmailSendResultDto> SendTemplateEmailAsync(
        string templateName,
        string recipientEmail,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken ct);
}