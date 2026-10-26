using Microsoft.Extensions.Logging;

namespace Notifications.Internal.Infrastructure;

internal interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}

internal sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // Trong môi trường dev/local: log ra console
        // Trong môi trường prod: tích hợp MailKit SMTP
        _logger.LogInformation("Sending email to {Recipient}: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
