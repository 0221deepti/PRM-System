using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRM.Application.DTOs.Notification;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailTemplateRepository _templates;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailTemplateRepository templates,
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger)
    {
        _templates = templates;
        _options = options;
        _logger = logger;
    }

    private static readonly System.Text.RegularExpressions.Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public async Task<EmailSendResultDto> SendTemplateEmailAsync(
        string templateName,
        string recipientEmail,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be empty.", nameof(templateName));

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return new EmailSendResultDto(recipientEmail ?? "", false, "Recipient email is empty.");

        var trimmedEmail = recipientEmail.Trim();
        if (!EmailRegex.IsMatch(trimmedEmail))
        {
            _logger.LogWarning("Email sending aborted: Recipient email '{Recipient}' is in an invalid format.", trimmedEmail);
            return new EmailSendResultDto(trimmedEmail, false, $"Invalid email address format: '{trimmedEmail}'.");
        }

        var template = await _templates.GetByNameAsync(templateName, ct)
            ?? throw new EntityNotFoundException($"Email template '{templateName}' not found.");

        var subject = Render(template.Subject, placeholders);
        var body = Render(template.Body, placeholders);
        var options = _options.Value;

        if (options == null || string.IsNullOrWhiteSpace(options.Host) || options.Port <= 0)
        {
            _logger.LogError("Email sending aborted: SMTP configuration is missing or invalid.");
            return new EmailSendResultDto(trimmedEmail, false, "SMTP configuration is missing or invalid.");
        }

        if (!options.Enabled)
        {
            _logger.LogInformation(
                "Email disabled. Template {Template} would be sent to {Recipient}. Subject: {Subject}",
                templateName,
                trimmedEmail,
                subject);

            return new EmailSendResultDto(trimmedEmail, true, null);
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(options.FromAddress, options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(trimmedEmail);

            using var client = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(options.Username, options.Password)
            };

            await client.SendMailAsync(message);
            return new EmailSendResultDto(trimmedEmail, true, null);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP failure sending email template {Template} to {Recipient}", templateName, trimmedEmail);
            return new EmailSendResultDto(trimmedEmail, false, $"SMTP failure: {smtpEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure sending email template {Template} to {Recipient}", templateName, trimmedEmail);
            return new EmailSendResultDto(trimmedEmail, false, $"Unexpected error: {ex.Message}");
        }
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        var rendered = template;
        foreach (var placeholder in placeholders)
        {
            rendered = rendered.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }
}