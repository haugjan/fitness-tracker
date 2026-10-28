namespace FitnessTracker.Test;

/// <summary>A clock that stands still, so a token expiry is an exact expectation.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
