namespace Ticketing.Infrastructure.Core.Inbox;

/// <summary>
/// Entity cho bảng InboxMessages — mỗi module có bảng riêng trong schema của mình.
/// Unique constraint (EventId, Handler) chống xử lý lặp.
/// </summary>
public sealed class InboxMessage
{
    public Guid Id { get; set; }

    /// <summary>
    /// EventId từ IntegrationEvent — khớp với EventId trong OutboxMessage.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Chuỗi đặt tay cho handler (ví dụ "SendTicketEmail").
    /// KHÔNG dùng Type.FullName để đổi namespace sau này không khiến event cũ bị xử lý lại.
    /// </summary>
    public string Handler { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}
