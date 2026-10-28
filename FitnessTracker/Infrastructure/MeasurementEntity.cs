using Azure;
using Azure.Data.Tables;
using FitnessTracker.Domain;

namespace FitnessTracker.Infrastructure;

internal sealed class MeasurementEntity : ITableEntity
{
    public string PartitionKey { get; set; } = TableNames.Owner;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public double? WeightKg { get; set; }
    public double? ScaleBodyFatPercent { get; set; }
    public double? WaistCm { get; set; }
    public double? AbdomenCm { get; set; }
    public double? HipCm { get; set; }
    public double? SkinfoldChestMm { get; set; }
    public double? SkinfoldAbdomenMm { get; set; }
    public double? SkinfoldThighMm { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }

    public static MeasurementEntity From(Measurement measurement) =>
        new()
        {
            PartitionKey = TableNames.Owner,
            RowKey = TableNames.RowKey(measurement.Date),
            WeightKg = measurement.Values.WeightKg,
            ScaleBodyFatPercent = measurement.Values.ScaleBodyFatPercent,
            WaistCm = measurement.Values.WaistCm,
            AbdomenCm = measurement.Values.AbdomenCm,
            HipCm = measurement.Values.HipCm,
            SkinfoldChestMm = measurement.Values.SkinfoldChestMm,
            SkinfoldAbdomenMm = measurement.Values.SkinfoldAbdomenMm,
            SkinfoldThighMm = measurement.Values.SkinfoldThighMm,
            Note = measurement.Values.Note,
            UpdatedUtc = measurement.UpdatedUtc,
        };

    public MeasurementDto ToDto() =>
        new()
        {
            Date = TableNames.DateFromRowKey(RowKey),
            WeightKg = WeightKg,
            ScaleBodyFatPercent = ScaleBodyFatPercent,
            WaistCm = WaistCm,
            AbdomenCm = AbdomenCm,
            HipCm = HipCm,
            SkinfoldChestMm = SkinfoldChestMm,
            SkinfoldAbdomenMm = SkinfoldAbdomenMm,
            SkinfoldThighMm = SkinfoldThighMm,
            Note = Note,
            UpdatedUtc = UpdatedUtc,
        };
}

internal sealed class ProfileEntity : ITableEntity
{
    public string PartitionKey { get; set; } = TableNames.Owner;
    public string RowKey { get; set; } = TableNames.ProfileRow;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public int SexValue { get; set; }
    public double? TargetWeightKg { get; set; }
    public double? TargetBodyFatPercent { get; set; }

    public static ProfileEntity From(UserProfile profile) =>
        new()
        {
            PartitionKey = TableNames.Owner,
            RowKey = TableNames.ProfileRow,
            BirthDate = profile.Values.BirthDate?.ToString("yyyy-MM-dd"),
            HeightCm = profile.Values.HeightCm,
            SexValue = (int)profile.Values.Sex,
            TargetWeightKg = profile.Values.TargetWeightKg,
            TargetBodyFatPercent = profile.Values.TargetBodyFatPercent,
        };

    public ProfileDto ToDto() =>
        new()
        {
            BirthDate = string.IsNullOrWhiteSpace(BirthDate)
                ? null
                : DateOnly.ParseExact(BirthDate, "yyyy-MM-dd"),
            HeightCm = HeightCm,
            Sex = (Sex)SexValue,
            TargetWeightKg = TargetWeightKg,
            TargetBodyFatPercent = TargetBodyFatPercent,
        };
}
