namespace FitnessTracker.Domain;

/// <summary>The constants behind the calculations, validated on the way in.</summary>
public sealed class UserProfile
{
    private UserProfile(ProfileDto values) => Values = values;

    public ProfileDto Values { get; }

    public static UserProfile Create(ProfileDto values, DateOnly today)
    {
        if (values.BirthDate is { } birthDate && (birthDate.Year < 1900 || birthDate > today))
        {
            throw new MeasurementValidationException("Das Geburtsdatum ist ungültig.");
        }

        if (values.HeightCm is { } height && (height < 100 || height > 250))
        {
            throw new MeasurementValidationException(
                "Die Körpergrösse muss zwischen 100 und 250 cm liegen."
            );
        }

        if (values.TargetWeightKg is { } weight && (weight < 20 || weight > 400))
        {
            throw new MeasurementValidationException(
                "Das Zielgewicht muss zwischen 20 und 400 kg liegen."
            );
        }

        if (values.TargetBodyFatPercent is { } fat && (fat < 3 || fat > 60))
        {
            throw new MeasurementValidationException(
                "Das Ziel für den Körperfettanteil muss zwischen 3 und 60 % liegen."
            );
        }

        return new UserProfile(values);
    }

    public ProfileDto ToDto() => Values;
}
