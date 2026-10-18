namespace KaloriTakip.Domain;

public enum Gender
{
    Male,
    Female
}

public enum ActivityLevel
{
    Sedentary,
    Light,
    Moderate,
    Active,
    VeryActive
}

public static class CalorieCalculator
{
    public const double MaxWeeklyLossKg = 1.0;
    private const int MinimumDailyCaloriesForFemale = 1200;
    private const int MinimumDailyCaloriesForMale = 1500;

    public static double CalculateBmr(double weightKg, double heightCm, int age, Gender gender)
    {
        ValidateProfile(weightKg, heightCm, age, gender);

        return 10 * weightKg + 6.25 * heightCm - 5 * age + (gender == Gender.Male ? 5 : -161);
    }

    public static int CalculateDailyLimit(
        double weightKg,
        double heightCm,
        int age,
        Gender gender,
        ActivityLevel activity,
        double weeklyLossKg)
    {
        ValidateProfile(weightKg, heightCm, age, gender);

        if (!Enum.IsDefined(activity))
        {
            throw new ArgumentOutOfRangeException(nameof(activity), "Geçerli bir aktivite seviyesi seçin.");
        }

        if (!double.IsFinite(weeklyLossKg) || weeklyLossKg < 0 || weeklyLossKg > MaxWeeklyLossKg)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weeklyLossKg),
                $"Haftalık hedef 0 ile {MaxWeeklyLossKg} kg arasında olmalıdır.");
        }

        var maintenanceCalories = CalculateBmr(weightKg, heightCm, age, gender) * GetActivityFactor(activity);
        var dailyDeficit = weeklyLossKg * 7700 / 7;
        var minimumDailyCalories = gender == Gender.Male
            ? MinimumDailyCaloriesForMale
            : MinimumDailyCaloriesForFemale;

        return (int)Math.Round(Math.Max(maintenanceCalories - dailyDeficit, minimumDailyCalories));
    }

    private static double GetActivityFactor(ActivityLevel activity) => activity switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        ActivityLevel.Active => 1.725,
        ActivityLevel.VeryActive => 1.9,
        _ => throw new ArgumentOutOfRangeException(nameof(activity))
    };

    private static void ValidateProfile(double weightKg, double heightCm, int age, Gender gender)
    {
        if (!double.IsFinite(weightKg) || weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Kilo pozitif bir sayı olmalıdır.");
        }

        if (!double.IsFinite(heightCm) || heightCm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heightCm), "Boy pozitif bir sayı olmalıdır.");
        }

        if (age < 18)
        {
            throw new ArgumentOutOfRangeException(nameof(age), "Bu hesaplama 18 yaş ve üzeri içindir.");
        }

        if (!Enum.IsDefined(gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender), "Geçerli bir cinsiyet seçin.");
        }
    }
}
