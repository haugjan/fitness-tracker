namespace FitnessTracker.Client.Services;

/// <summary>
/// The three skinfold sites the formula expects. Men are pinched at chest, abdomen and
/// thigh, women at triceps, suprailiac and thigh, so the labels follow the profile.
/// </summary>
public static class Sites
{
    public static string First(Sex sex) => sex == Sex.Female ? "Trizeps" : "Brust";

    public static string Second(Sex sex) => sex == Sex.Female ? "Hüftkamm" : "Bauch";

    public static string Third(Sex sex) => "Oberschenkel";

    public static string FirstShort(Sex sex) => sex == Sex.Female ? "Trizeps" : "Brust";

    public static string SecondShort(Sex sex) => sex == Sex.Female ? "Hüfte" : "Bauch";

    public static string ThirdShort(Sex sex) => "Bein";
}
