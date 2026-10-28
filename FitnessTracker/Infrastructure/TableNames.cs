namespace FitnessTracker.Infrastructure;

/// <summary>
/// The app holds the data of exactly one person, so every row shares one partition.
/// Dates are the row keys, which keeps a range query inside a single partition.
/// </summary>
internal static class TableNames
{
    public const string Measurements = "measurements";
    public const string Profile = "profile";
    public const string Owner = "me";
    public const string ProfileRow = "profile";

    public static string RowKey(DateOnly date) => date.ToString("yyyy-MM-dd");

    public static DateOnly DateFromRowKey(string rowKey) => DateOnly.ParseExact(rowKey, "yyyy-MM-dd");
}
