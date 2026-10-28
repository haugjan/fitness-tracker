using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Measurements;

public static class GetMeasurementAction
{
    public sealed record Query(DateOnly Date);

    public interface IHandler
    {
        Task<MeasurementDto?> HandleAsync(Query query, CancellationToken cancellationToken);
    }

    internal sealed class Handler(IMeasurementRepository repository) : IHandler
    {
        public Task<MeasurementDto?> HandleAsync(
            Query query,
            CancellationToken cancellationToken
        ) => repository.GetAsync(query.Date, cancellationToken);
    }
}
