using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Profile;

public static class GetProfileAction
{
    public sealed record Query;

    public interface IHandler
    {
        Task<ProfileDto> HandleAsync(Query query, CancellationToken cancellationToken);
    }

    internal sealed class Handler(IProfileRepository repository) : IHandler
    {
        public async Task<ProfileDto> HandleAsync(
            Query query,
            CancellationToken cancellationToken
        ) => await repository.GetAsync(cancellationToken) ?? new ProfileDto();
    }
}
