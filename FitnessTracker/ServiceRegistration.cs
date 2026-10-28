using FitnessTracker.Features;
using FitnessTracker.Features.Measurements;
using FitnessTracker.Features.Profile;
using Microsoft.AspNetCore.RateLimiting;

namespace FitnessTracker;

public static class ServiceRegistration
{
    /// <summary>
    /// One fixed window for the whole login endpoint. For a single user app that is stricter
    /// than a per address limit, and ten attempts per five minutes is plenty for the owner.
    /// </summary>
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LoginAction.IHandler, LoginAction.Handler>();
        services.AddScoped<SaveMeasurementAction.IHandler, SaveMeasurementAction.Handler>();
        services.AddScoped<GetMeasurementAction.IHandler, GetMeasurementAction.Handler>();
        services.AddScoped<ListMeasurementsAction.IHandler, ListMeasurementsAction.Handler>();
        services.AddScoped<DeleteMeasurementAction.IHandler, DeleteMeasurementAction.Handler>();
        services.AddScoped<GetProfileAction.IHandler, GetProfileAction.Handler>();
        services.AddScoped<SaveProfileAction.IHandler, SaveProfileAction.Handler>();

        return services;
    }

    public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(
                LoginRateLimitPolicy,
                limiter =>
                {
                    limiter.Window = TimeSpan.FromMinutes(5);
                    limiter.PermitLimit = 10;
                    limiter.QueueLimit = 0;
                }
            );
        });
}
