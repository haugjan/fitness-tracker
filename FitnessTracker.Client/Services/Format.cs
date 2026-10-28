using System.Globalization;

namespace FitnessTracker.Client.Services;

/// <summary>
/// German names and Swiss number shapes, written out instead of pulled from culture data,
/// so the WebAssembly bundle stays free of globalization surprises.
/// </summary>
public static class Format
{
    private static readonly string[] Weekdays =
    [
        "Sonntag",
        "Montag",
        "Dienstag",
        "Mittwoch",
        "Donnerstag",
        "Freitag",
        "Samstag",
    ];

    private static readonly string[] WeekdaysShort =
    [
        "So",
        "Mo",
        "Di",
        "Mi",
        "Do",
        "Fr",
        "Sa",
    ];

    private static readonly string[] Months =
    [
        "Januar",
        "Februar",
        "März",
        "April",
        "Mai",
        "Juni",
        "Juli",
        "August",
        "September",
        "Oktober",
        "November",
        "Dezember",
    ];

    public static string LongDate(DateOnly date) =>
        $"{Weekdays[(int)date.DayOfWeek]}, {date.Day}. {Months[date.Month - 1]}";

    public static string ShortDate(DateOnly date) =>
        $"{WeekdaysShort[(int)date.DayOfWeek]} {date.Day:00}.{date.Month:00}.";

    public static string MonthLabel(DateOnly date) => $"{Months[date.Month - 1][..3]} {date.Year}";

    public static string DayMonth(DateOnly date) => $"{date.Day:00}.{date.Month:00}.";

    public static int WeekOfYear(DateOnly date) => ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

    public static string Number(double value, int digits) =>
        value.ToString("F" + digits, CultureInfo.InvariantCulture);

    public static string? Number(double? value, int digits) =>
        value is null ? null : Number(value.Value, digits);

    /// <summary>A change, written the way a person says it: with a sign in front.</summary>
    public static string Signed(double value, int digits, string unit)
    {
        var sign = value > 0 ? "+" : value < 0 ? "−" : "±";
        return $"{sign}{Number(Math.Abs(value), digits)} {unit}".Trim();
    }

    /// <summary>Accepts both the comma and the point, so the keyboard does not matter.</summary>
    public static double? ParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;
    }
}
