using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Internal.Application;
using Notifications.Internal.Infrastructure;
using Ticketing.Contracts;
using Ticketing.Infrastructure.Core;

namespace Notifications.Internal.Api;

/// <summary>
/// Điểm vào duy nhất (public) của module Notifications.
/// Module này chỉ lắng nghe Integration Events (không có HTTP endpoints).
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Đăng ký DbContext với schema "notifications" riêng biệt (Điểm 8 & BaseDbContext)
        services.AddModuleDbContext<NotificationsDbContext>(
            connectionStringName: "DefaultConnection",
            schema: NotificationsDbContext.SchemaName,
            configuration: configuration);

        // 2. Dịch vụ gửi email
        services.AddScoped<IEmailService, EmailService>();

        // 3. Đăng ký Integration Event Handler (Điểm 2: lắng nghe TicketIssued, Điểm 4: Outbox/Inbox)
        services.RegisterIntegrationEventHandler<TicketIssuedIntegrationEvent, TicketIssuedEventHandler>();

        // 4. Quét validators bao gồm internal types (Khắc phục rủi ro #1)
        services.AddModuleValidators(typeof(NotificationsModule).Assembly);

        return services;
    }
}
