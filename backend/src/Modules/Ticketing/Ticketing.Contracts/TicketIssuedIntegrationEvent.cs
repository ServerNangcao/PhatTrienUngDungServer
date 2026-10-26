using Ticketing.SharedKernel;

namespace Ticketing.Contracts;

/// <summary>
/// DTO thông tin vé được phát hành để gửi email.
/// </summary>
public sealed record IssuedTicketDto
{
    public Guid TicketId { get; init; }
    public string TicketCode { get; init; } = string.Empty;
    public string TicketTypeName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string QrPayload { get; init; } = string.Empty;
}

/// <summary>
/// Integration Event phát ra từ module Ticketing khi vé được phát hành thành công sau thanh toán.
/// Notifications lắng nghe event này để gửi email vé điện tử (Điểm 2).
/// CustomerEmail được sao chép sẵn từ Order để Notifications không cần gọi sang Identity.
/// </summary>
public sealed record TicketIssuedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid TargetEventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public IReadOnlyList<IssuedTicketDto> Tickets { get; init; } = [];
}
