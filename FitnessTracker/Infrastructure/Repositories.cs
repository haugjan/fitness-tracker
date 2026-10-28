using FitnessTracker.Domain;

namespace FitnessTracker.Infrastructure;

/// <summary>
/// Reads hand out DTOs, writes take a validated domain object. That keeps validation on the
/// write path only, so tightening a rule later never locks an old row out of the app.
/// </summary>
public interface IMeasurementRepository
{
    Task<MeasurementDto?> GetAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementDto>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    );

    Task UpsertAsync(Measurement measurement, CancellationToken cancellationToken);

    Task DeleteAsync(DateOnly date, CancellationToken cancellationToken);
}

public interface IProfileRepository
{
    Task<ProfileDto?> GetAsync(CancellationToken cancellationToken);

    Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken);
}
