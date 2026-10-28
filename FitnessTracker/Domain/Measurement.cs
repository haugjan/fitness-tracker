namespace FitnessTracker.Domain;

/// <summary>One day of measurements, validated on the way in.</summary>
public sealed class Measurement
{
    private Measurement(DateOnly date, MeasurementInput values, DateTimeOffset updatedUtc)
    {
        Date = date;
        Values = values;
        UpdatedUtc = updatedUtc;
    }

    public DateOnly Date { get; }

    public MeasurementInput Values { get; }

    public DateTimeOffset UpdatedUtc { get; }

    public static Measurement Create(
        DateOnly date,
        MeasurementInput values,
        DateTimeOffset updatedUtc
    )
    {
        if (date.Year < 2000 || date > DateOnly.FromDateTime(updatedUtc.UtcDateTime.AddDays(1)))
        {
            throw new MeasurementValidationException($"Das Datum {date:dd.MM.yyyy} ist ungültig.");
        }

        Require(values.WeightKg, 20, 400, "Gewicht", "kg");
        Require(values.ScaleBodyFatPercent, 1, 70, "Körperfett der Waage", "%");
        Require(values.WaistCm, 30, 250, "Taillenumfang", "cm");
        Require(values.AbdomenCm, 30, 250, "Bauchumfang", "cm");
        Require(values.HipCm, 30, 250, "Hüftumfang", "cm");
        Require(values.SkinfoldChestMm, 1, 100, "Falte an der Brust", "mm");
        Require(values.SkinfoldAbdomenMm, 1, 100, "Falte am Bauch", "mm");
        Require(values.SkinfoldThighMm, 1, 100, "Falte am Oberschenkel", "mm");

        if (values.Note is { Length: > 280 })
        {
            throw new MeasurementValidationException("Die Notiz darf höchstens 280 Zeichen haben.");
        }

        return new Measurement(date, values, updatedUtc);
    }

    public MeasurementDto ToDto() =>
        new()
        {
            Date = Date,
            WeightKg = Values.WeightKg,
            ScaleBodyFatPercent = Values.ScaleBodyFatPercent,
            WaistCm = Values.WaistCm,
            AbdomenCm = Values.AbdomenCm,
            HipCm = Values.HipCm,
            SkinfoldChestMm = Values.SkinfoldChestMm,
            SkinfoldAbdomenMm = Values.SkinfoldAbdomenMm,
            SkinfoldThighMm = Values.SkinfoldThighMm,
            Note = Values.Note,
            UpdatedUtc = UpdatedUtc,
        };

    private static void Require(double? value, double min, double max, string label, string unit)
    {
        if (value is null)
        {
            return;
        }

        if (double.IsNaN(value.Value) || value < min || value > max)
        {
            throw new MeasurementValidationException(
                $"{label} muss zwischen {min:0.#} und {max:0.#} {unit} liegen."
            );
        }
    }
}
