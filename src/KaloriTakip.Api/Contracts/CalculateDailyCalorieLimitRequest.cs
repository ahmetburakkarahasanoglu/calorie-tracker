using System.ComponentModel.DataAnnotations;
using KaloriTakip.Domain;

namespace KaloriTakip.Api.Contracts;

/// <summary>Personal information and weight-loss goal used to calculate a daily calorie limit.</summary>
public sealed record CalculateDailyCalorieLimitRequest
{
    [Range(0.1, 500)]
    public required double WeightKg { get; init; }

    [Range(50, 300)]
    public required double HeightCm { get; init; }

    [Range(18, 120)]
    public required int Age { get; init; }

    public required Gender Gender { get; init; }

    public required ActivityLevel Activity { get; init; }

    [Range(0, CalorieCalculator.MaxWeeklyLossKg)]
    public required double WeeklyLossKg { get; init; }
}
