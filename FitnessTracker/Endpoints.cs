using FitnessTracker.Features;
using FitnessTracker.Features.Measurements;
using FitnessTracker.Features.Profile;
using Microsoft.AspNetCore.RateLimiting;

namespace FitnessTracker;

public static class Endpoints
{
    public static void MapTrackerEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/auth/login",
                (LoginRequest request, LoginAction.IHandler handler) =>
                {
                    var response = handler.Handle(new LoginAction.Command(request.Pin));
                    return response is null ? Results.Unauthorized() : Results.Ok(response);
                }
            )
            .AllowAnonymous()
            .RequireRateLimiting(ServiceRegistration.LoginRateLimitPolicy);

        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet(
            "/measurements",
            async (
                DateOnly? from,
                DateOnly? to,
                ListMeasurementsAction.IHandler handler,
                TimeProvider time,
                CancellationToken cancellationToken
            ) =>
            {
                var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
                var query = new ListMeasurementsAction.Query(
                    from ?? today.AddYears(-1),
                    to ?? today
                );

                return await handler.HandleAsync(query, cancellationToken);
            }
        );

        api.MapGet(
            "/measurements/{date}",
            async (
                DateOnly date,
                GetMeasurementAction.IHandler handler,
                CancellationToken cancellationToken
            ) =>
            {
                var measurement = await handler.HandleAsync(
                    new GetMeasurementAction.Query(date),
                    cancellationToken
                );

                return measurement is null ? Results.NotFound() : Results.Ok(measurement);
            }
        );

        api.MapPut(
            "/measurements/{date}",
            async (
                DateOnly date,
                MeasurementInput input,
                SaveMeasurementAction.IHandler handler,
                CancellationToken cancellationToken
            ) =>
                await handler.HandleAsync(
                    new SaveMeasurementAction.Command(date, input),
                    cancellationToken
                )
        );

        api.MapDelete(
            "/measurements/{date}",
            async (
                DateOnly date,
                DeleteMeasurementAction.IHandler handler,
                CancellationToken cancellationToken
            ) =>
            {
                await handler.HandleAsync(
                    new DeleteMeasurementAction.Command(date),
                    cancellationToken
                );

                return Results.NoContent();
            }
        );

        api.MapGet(
            "/profile",
            async (GetProfileAction.IHandler handler, CancellationToken cancellationToken) =>
                await handler.HandleAsync(new GetProfileAction.Query(), cancellationToken)
        );

        api.MapPut(
            "/profile",
            async (
                ProfileDto profile,
                SaveProfileAction.IHandler handler,
                CancellationToken cancellationToken
            ) =>
                await handler.HandleAsync(
                    new SaveProfileAction.Command(profile),
                    cancellationToken
                )
        );
    }
}
