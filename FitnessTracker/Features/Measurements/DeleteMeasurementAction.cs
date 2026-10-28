using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Measurements;

public static class DeleteMeasurementAction
{
    public sealed record Command(DateOnly Date);

    public interface IHandler
    {
        Task HandleAsync(Command command, CancellationToken cancellationToken);
    }

    internal sealed class Handler(IMeasurementRepository repository) : IHandler
    {
        public Task HandleAsync(Command command, CancellationToken cancellationToken) =>
            repository.DeleteAsync(command.Date, cancellationToken);
    }
}
