using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Internal.Api;

/// <summary>
/// Điểm vào duy nhất (public) của module Events.
/// Đăng ký DI và map endpoints.
/// </summary>
public static class EventsModule
{
    public static IServiceCollection AddEventsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Đăng ký DbContext, Services, Validators, EventHandlers
        return services;
    }

    public static IEndpointRouteBuilder MapEventsEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Map các API endpoints
        return app;
    }
}
