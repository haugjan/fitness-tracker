using FitnessTracker.Domain;
using FitnessTracker.Features.Measurements;
using FitnessTracker.Infrastructure;
using NSubstitute;

namespace FitnessTracker.Test;

public class SaveMeasurementActionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 7, 10, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_saved_day_reaches_the_repository_and_comes_back_as_written()
    {
        var repository = Substitute.For<IMeasurementRepository>();
        var handler = new SaveMeasurementAction.Handler(repository, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new SaveMeasurementAction.Command(
                new DateOnly(2026, 10, 27),
                new MeasurementInput { WeightKg = 82.4, SkinfoldChestMm = 8 }
            ),
            CancellationToken.None
        );

        result.WeightKg.ShouldBe(82.4);
        result.UpdatedUtc.ShouldBe(Now);

        await repository
            .Received(1)
            .UpsertAsync(
                Arg.Is<Measurement>(measurement =>
                    measurement.Date == new DateOnly(2026, 10, 27)
                    && measurement.Values.WeightKg == 82.4
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task A_rejected_value_never_reaches_the_repository()
    {
        var repository = Substitute.For<IMeasurementRepository>();
        var handler = new SaveMeasurementAction.Handler(repository, new FixedTimeProvider(Now));

        var act = () =>
            handler.HandleAsync(
                new SaveMeasurementAction.Command(
                    new DateOnly(2026, 10, 27),
                    new MeasurementInput { WaistCm = 12 }
                ),
                CancellationToken.None
            );

        await act.ShouldThrowAsync<MeasurementValidationException>();

        await repository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<Measurement>(), Arg.Any<CancellationToken>());
    }
}
