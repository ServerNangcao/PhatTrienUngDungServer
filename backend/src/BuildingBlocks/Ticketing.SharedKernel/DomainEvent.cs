namespace Ticketing.SharedKernel;

/// <summary>
/// Marker interface cho Domain Event (nội bộ trong một module).
/// Domain Events được phát ra trong cùng transaction với thay đổi aggregate.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base record cho Domain Events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
