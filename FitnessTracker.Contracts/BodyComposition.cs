namespace FitnessTracker.Contracts;

/// <summary>Everything that is derived from a measurement rather than entered by hand.</summary>
public sealed record DerivedValues
{
    /// <summary>Body fat from the three skinfolds, Jackson and Pollock plus Siri.</summary>
    public double? BodyFatPercent { get; init; }

    public double? SkinfoldSumMm { get; init; }
    public double? FatMassKg { get; init; }
    public double? LeanMassKg { get; init; }
    public double? WaistToHeightRatio { get; init; }
    public double? WaistToHipRatio { get; init; }
    public int? AgeYears { get; init; }
}

/// <summary>
/// Body fat from three skinfolds: Jackson and Pollock 1978 for the body density,
/// Siri 1961 to turn density into a percentage.
/// Men are measured at chest, abdomen and thigh, women at triceps, suprailiac and thigh.
/// </summary>
public static class BodyComposition
{
    public static DerivedValues Calculate(MeasurementDto measurement, ProfileDto profile)
    {
        var age = AgeAt(profile.BirthDate, measurement.Date);
        var sum = SkinfoldSum(measurement);
        var bodyFat = BodyFatFromSkinfolds(profile.Sex, age, sum);

        return new DerivedValues
        {
            AgeYears = age,
            SkinfoldSumMm = sum,
            BodyFatPercent = bodyFat is null ? null : Round(bodyFat.Value, 1),
            FatMassKg = Multiply(measurement.WeightKg, bodyFat),
            LeanMassKg = Multiply(measurement.WeightKg, bodyFat is null ? null : 100d - bodyFat.Value),
            WaistToHeightRatio = Divide(measurement.WaistCm, profile.HeightCm, 2),
            WaistToHipRatio = Divide(measurement.WaistCm, measurement.HipCm, 2),
        };
    }

    public static double? SkinfoldSum(MeasurementDto measurement)
    {
        if (
            measurement.SkinfoldChestMm is not { } chest
            || measurement.SkinfoldAbdomenMm is not { } abdomen
            || measurement.SkinfoldThighMm is not { } thigh
        )
        {
            return null;
        }

        return chest + abdomen + thigh;
    }

    public static double? BodyFatFromSkinfolds(Sex sex, int? ageYears, double? skinfoldSumMm)
    {
        if (ageYears is not { } age || skinfoldSumMm is not { } sum || sum <= 0)
        {
            return null;
        }

        var density = sex switch
        {
            Sex.Female => 1.0994921
                - (0.0009929 * sum)
                + (0.0000023 * sum * sum)
                - (0.0001392 * age),
            _ => 1.10938 - (0.0008267 * sum) + (0.0000016 * sum * sum) - (0.0002574 * age),
        };

        if (density <= 0)
        {
            return null;
        }

        var percent = (495d / density) - 450d;
        return percent is <= 0 or > 75 ? null : percent;
    }

    public static int? AgeAt(DateOnly? birthDate, DateOnly onDate)
    {
        if (birthDate is not { } birth || birth > onDate)
        {
            return null;
        }

        var age = onDate.Year - birth.Year;
        if (onDate < birth.AddYears(age))
        {
            age--;
        }

        return age;
    }

    private static double? Multiply(double? weightKg, double? percent) =>
        weightKg is { } kg && percent is { } p ? Round(kg * p / 100d, 1) : null;

    private static double? Divide(double? value, double? by, int digits) =>
        value is { } v && by is { } b && b > 0 ? Round(v / b, digits) : null;

    private static double Round(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);
}
