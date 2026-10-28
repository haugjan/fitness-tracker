using FitnessTracker.Domain;

namespace FitnessTracker.Test;

public class MeasurementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 7, 10, 0, TimeSpan.Zero);

    [Fact]
    public void A_day_with_only_a_weight_is_a_valid_entry()
    {
        var measurement = Measurement.Create(
            new DateOnly(2026, 10, 27),
            new MeasurementInput { WeightKg = 82.4 },
            Now
        );

        measurement.ToDto().WeightKg.ShouldBe(82.4);
        measurement.ToDto().WaistCm.ShouldBeNull();
    }

    [Theory]
    [InlineData(19)]
    [InlineData(401)]
    public void A_weight_outside_the_human_range_is_rejected(double weight)
    {
        var act = () =>
            Measurement.Create(
                new DateOnly(2026, 10, 27),
                new MeasurementInput { WeightKg = weight },
                Now
            );

        act.ShouldThrow<MeasurementValidationException>().Message.ShouldContain("Gewicht");
    }

    [Fact]
    public void A_skinfold_of_zero_is_rejected()
    {
        var act = () =>
            Measurement.Create(
                new DateOnly(2026, 10, 27),
                new MeasurementInput { SkinfoldChestMm = 0 },
                Now
            );

        act.ShouldThrow<MeasurementValidationException>().Message.ShouldContain("Brust");
    }

    [Fact]
    public void A_note_longer_than_the_limit_is_rejected()
    {
        var act = () =>
            Measurement.Create(
                new DateOnly(2026, 10, 27),
                new MeasurementInput { Note = new string('a', 281) },
                Now
            );

        act.ShouldThrow<MeasurementValidationException>().Message.ShouldContain("Notiz");
    }

    [Fact]
    public void A_date_in_the_future_is_rejected()
    {
        var act = () =>
            Measurement.Create(
                new DateOnly(2026, 11, 5),
                new MeasurementInput { WeightKg = 82.4 },
                Now
            );

        act.ShouldThrow<MeasurementValidationException>();
    }

    [Fact]
    public void Yesterday_and_today_are_both_allowed()
    {
        Measurement
            .Create(new DateOnly(2026, 10, 26), new MeasurementInput { WeightKg = 82 }, Now)
            .Date.ShouldBe(new DateOnly(2026, 10, 26));

        Measurement
            .Create(new DateOnly(2026, 10, 27), new MeasurementInput { WeightKg = 82 }, Now)
            .UpdatedUtc.ShouldBe(Now);
    }
}
