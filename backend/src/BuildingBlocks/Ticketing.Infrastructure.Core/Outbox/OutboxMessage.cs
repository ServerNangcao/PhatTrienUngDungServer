namespace Ticketing.Infrastructure.Core.Outbox;

/// <summary>
/// Entity cho bảng OutboxMessages — mỗi module có bảng riêng trong schema của mình.
/// Worker quét bảng này để phát IntegrationEvent sang các module khác.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>
    /// Tên event type (lấy từ class name trong Contracts, không dùng FullName để tránh
    /// lỗi khi đổi namespace).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// EventId của IntegrationEvent — dùng để Inbox nhận diện trùng lặp.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Nội dung event đã serialize thành JSON.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Null = chưa xử lý. Worker set giá trị khi đã giao xong.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Thông tin lỗi nếu xử lý thất bại (để debug).
    /// </summary>
    public string? Error { get; set; }
}
