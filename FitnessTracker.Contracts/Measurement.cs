namespace FitnessTracker.Contracts;

/// <summary>One day of body measurements. Every value is optional, a day may hold only the weight.</summary>
public sealed record MeasurementDto
{
    public required DateOnly Date { get; init; }

    public double? WeightKg { get; init; }

    /// <summary>Body fat percentage as reported by the bioimpedance scale.</summary>
    public double? ScaleBodyFatPercent { get; init; }

    public double? WaistCm { get; init; }
    public double? AbdomenCm { get; init; }
    public double? HipCm { get; init; }

    /// <summary>Skinfold at the chest (men) or triceps (women), in millimetres.</summary>
    public double? SkinfoldChestMm { get; init; }

    public double? SkinfoldAbdomenMm { get; init; }
    public double? SkinfoldThighMm { get; init; }

    public string? Note { get; init; }

    public DateTimeOffset UpdatedUtc { get; init; }
}

/// <summary>Values submitted for one day. The date comes from the route.</summary>
public sealed record MeasurementInput
{
    public double? WeightKg { get; init; }
    public double? ScaleBodyFatPercent { get; init; }
    public double? WaistCm { get; init; }
    public double? AbdomenCm { get; init; }
    public double? HipCm { get; init; }
    public double? SkinfoldChestMm { get; init; }
    public double? SkinfoldAbdomenMm { get; init; }
    public double? SkinfoldThighMm { get; init; }
    public string? Note { get; init; }
}
