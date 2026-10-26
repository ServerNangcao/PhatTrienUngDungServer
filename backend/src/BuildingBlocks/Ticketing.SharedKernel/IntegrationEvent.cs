namespace Ticketing.SharedKernel;

/// <summary>
/// Base class cho Integration Events truyền giữa các module qua Outbox.
/// EventId cố định để Inbox nhận diện event trùng lặp.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Id cố định cho mỗi event, dùng để chống xử lý lặp (Inbox deduplication).
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Thời điểm event được tạo (UTC).
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
