using Ticketing.SharedKernel;

namespace Notifications.Internal.Domain;

internal enum EmailStatus
{
    Sent,
    Failed
}

internal sealed class EmailLog : BaseEntity<Guid>
{
    public string RecipientEmail { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public EmailStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAt { get; private set; }

    private EmailLog()
    {
    }

    public static EmailLog CreateSuccess(string recipientEmail, string subject, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            Status = EmailStatus.Sent,
            SentAt = DateTime.UtcNow
        };

    public static EmailLog CreateFailure(string recipientEmail, string subject, string body, string errorMessage) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            Status = EmailStatus.Failed,
            ErrorMessage = errorMessage,
            SentAt = DateTime.UtcNow
        };
}
