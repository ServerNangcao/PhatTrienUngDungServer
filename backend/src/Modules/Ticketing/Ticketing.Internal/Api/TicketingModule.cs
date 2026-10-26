using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ticketing.Internal.Api;

/// <summary>
/// Điểm vào duy nhất (public) của module Ticketing.
/// Đăng ký DI và map endpoints.
/// </summary>
public static class TicketingModule
{
    public static IServiceCollection AddTicketingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Đăng ký DbContext, Services, Validators, EventHandlers
        return services;
    }

    public static IEndpointRouteBuilder MapTicketingEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Map các API endpoints
        return app;
    }
}
