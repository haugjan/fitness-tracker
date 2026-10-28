namespace FitnessTracker.Contracts;

public sealed record LoginRequest(string Pin);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresUtc);
