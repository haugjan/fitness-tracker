namespace FitnessTracker.Domain;

/// <summary>A value outside the range a human body can plausibly have. The message reaches the UI.</summary>
public sealed class MeasurementValidationException(string message) : Exception(message);
