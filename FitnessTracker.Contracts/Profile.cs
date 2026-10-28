namespace FitnessTracker.Contracts;

public enum Sex
{
    Male = 0,
    Female = 1,
}

/// <summary>
/// The constants the calculations need: age and sex for the skinfold formula,
/// height for the waist to height ratio.
/// </summary>
public sealed record ProfileDto
{
    public DateOnly? BirthDate { get; init; }
    public double? HeightCm { get; init; }
    public Sex Sex { get; init; } = Sex.Male;
    public double? TargetWeightKg { get; init; }
    public double? TargetBodyFatPercent { get; init; }
}
