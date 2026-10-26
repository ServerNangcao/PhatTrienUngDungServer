namespace Ticketing.SharedKernel;

/// <summary>
/// Base entity cho tất cả các aggregate root và entity trong hệ thống.
/// </summary>
public abstract class BaseEntity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
