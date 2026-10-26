using Ticketing.SharedKernel;

namespace Ticketing.Contracts;

/// <summary>
/// Integration Event phát ra từ module Ticketing khi hạng vé được tạo, cập nhật hoặc đổi giá.
/// Module Events lắng nghe event này để cập nhật MinPrice/MaxPrice và xóa cache danh sách sự kiện (Điểm 5).
/// </summary>
public sealed record TicketTypesChangedIntegrationEvent : IntegrationEvent
{
    public Guid TargetEventId { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
}
