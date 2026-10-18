namespace KaloriTakip.Api.Contracts;

/// <summary>A previously saved daily calorie calculation for a user.</summary>
public sealed record CalorieLimitHistoryResponse(
    long RecordId,
    int DailyCalorieLimit,
    double WeightKg,
    double WeeklyLossKg,
    DateTimeOffset CreatedAt);
