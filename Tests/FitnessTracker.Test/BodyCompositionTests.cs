namespace FitnessTracker.Test;

public class BodyCompositionTests
{
    private static readonly ProfileDto ManAtForty = new()
    {
        BirthDate = new DateOnly(1986, 5, 18),
        HeightCm = 181,
        Sex = Sex.Male,
    };

    private static MeasurementDto Measurement(
        double? chest = 8,
        double? abdomen = 17,
        double? thigh = 12
    ) =>
        new()
        {
            Date = new DateOnly(2026, 10, 27),
            WeightKg = 82.4,
            WaistCm = 84,
            HipCm = 97,
            SkinfoldChestMm = chest,
            SkinfoldAbdomenMm = abdomen,
            SkinfoldThighMm = thigh,
        };

    [Fact]
    public void Skinfolds_of_a_man_follow_Jackson_Pollock_and_Siri()
    {
        var derived = BodyComposition.Calculate(Measurement(), ManAtForty);

        derived.SkinfoldSumMm.ShouldBe(37);
        derived.AgeYears.ShouldBe(40);
        derived.BodyFatPercent!.Value.ShouldBe(12.3, 0.05);
    }

    [Fact]
    public void Skinfolds_of_a_woman_use_the_other_constants()
    {
        var profile = ManAtForty with { BirthDate = new DateOnly(1991, 3, 2), Sex = Sex.Female };
        var measurement = Measurement(chest: 18, abdomen: 20, thigh: 12);

        var derived = BodyComposition.Calculate(measurement, profile);

        derived.AgeYears.ShouldBe(35);
        derived.BodyFatPercent!.Value.ShouldBe(21.1, 0.05);
    }

    [Fact]
    public void Mass_and_ratios_come_from_the_same_measurement()
    {
        var derived = BodyComposition.Calculate(Measurement(), ManAtForty);

        derived.FatMassKg!.Value.ShouldBe(10.2, 0.05);
        derived.LeanMassKg!.Value.ShouldBe(72.2, 0.05);
        derived.WaistToHeightRatio.ShouldBe(0.46);
        derived.WaistToHipRatio.ShouldBe(0.87);
    }

    [Fact]
    public void One_missing_skinfold_leaves_the_percentage_open()
    {
        var derived = BodyComposition.Calculate(Measurement(thigh: null), ManAtForty);

        derived.SkinfoldSumMm.ShouldBeNull();
        derived.BodyFatPercent.ShouldBeNull();
        derived.FatMassKg.ShouldBeNull();
    }

    [Fact]
    public void Without_a_birth_date_the_formula_stays_silent()
    {
        var derived = BodyComposition.Calculate(Measurement(), ManAtForty with { BirthDate = null });

        derived.AgeYears.ShouldBeNull();
        derived.BodyFatPercent.ShouldBeNull();
        derived.WaistToHeightRatio.ShouldBe(0.46);
    }

    [Theory]
    [InlineData(2026, 5, 17, 39)]
    [InlineData(2026, 5, 18, 40)]
    [InlineData(2026, 12, 31, 40)]
    public void Age_turns_over_on_the_birthday(int year, int month, int day, int expected) =>
        BodyComposition
            .AgeAt(new DateOnly(1986, 5, 18), new DateOnly(year, month, day))
            .ShouldBe(expected);
}
