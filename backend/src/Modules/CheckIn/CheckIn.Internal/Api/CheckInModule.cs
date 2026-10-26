using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheckIn.Internal.Api;

/// <summary>
/// Điểm vào duy nhất (public) của module CheckIn.
/// Đăng ký DI và map endpoints.
/// </summary>
public static class CheckInModule
{
    public static IServiceCollection AddCheckInModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Đăng ký DbContext, Services, Validators, EventHandlers
        return services;
    }

    public static IEndpointRouteBuilder MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Map các API endpoints
        return app;
    }
}
