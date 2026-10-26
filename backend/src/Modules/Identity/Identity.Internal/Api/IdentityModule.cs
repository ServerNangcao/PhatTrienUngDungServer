using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Internal.Api;

/// <summary>
/// Điểm vào duy nhất (public) của module Identity.
/// Đăng ký DI và map endpoints.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Đăng ký DbContext, Services, Validators, EventHandlers
        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Map các API endpoints
        return app;
    }
}
