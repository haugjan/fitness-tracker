using FitnessTracker.Domain;
using FitnessTracker.Infrastructure;

namespace FitnessTracker.Features.Profile;

public static class SaveProfileAction
{
    public sealed record Command(ProfileDto Values);

    public interface IHandler
    {
        Task<ProfileDto> HandleAsync(Command command, CancellationToken cancellationToken);
    }

    internal sealed class Handler(IProfileRepository repository, TimeProvider time) : IHandler
    {
        public async Task<ProfileDto> HandleAsync(
            Command command,
            CancellationToken cancellationToken
        )
        {
            var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
            var profile = UserProfile.Create(command.Values, today);
            await repository.UpsertAsync(profile, cancellationToken);
            return profile.ToDto();
        }
    }
}
