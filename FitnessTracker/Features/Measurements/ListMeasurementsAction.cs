using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Measurements;

public static class ListMeasurementsAction
{
    public sealed record Query(DateOnly From, DateOnly To);

    public interface IHandler
    {
        Task<IReadOnlyList<MeasurementDto>> HandleAsync(
            Query query,
            CancellationToken cancellationToken
        );
    }

    internal sealed class Handler(IMeasurementRepository repository) : IHandler
    {
        public Task<IReadOnlyList<MeasurementDto>> HandleAsync(
            Query query,
            CancellationToken cancellationToken
        ) => repository.ListAsync(query.From, query.To, cancellationToken);
    }
}
