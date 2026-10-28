using FitnessTracker.Domain;
using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Measurements;

public static class SaveMeasurementAction
{
    public sealed record Command(DateOnly Date, MeasurementInput Values);

    public interface IHandler
    {
        Task<MeasurementDto> HandleAsync(Command command, CancellationToken cancellationToken);
    }

    internal sealed class Handler(IMeasurementRepository repository, TimeProvider time) : IHandler
    {
        public async Task<MeasurementDto> HandleAsync(
            Command command,
            CancellationToken cancellationToken
        )
        {
            var measurement = Measurement.Create(command.Date, command.Values, time.GetUtcNow());
            await repository.UpsertAsync(measurement, cancellationToken);
            return measurement.ToDto();
        }
    }
}
