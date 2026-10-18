namespace KaloriTakip.Api.Contracts;

/// <summary>The calculated daily calorie target and its saved record identifier.</summary>
public sealed record DailyCalorieLimitResponse(
    long UserId,
    long RecordId,
    int DailyCalorieLimit,
    DateTimeOffset CreatedAt);
