using KaloriTakip.Domain;

namespace KaloriTakip.Application.Services;

public interface ICalorieLimitRepository
{
    Task<long> SaveAsync(NewCalorieLimitEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalorieLimitHistoryItem>?> GetHistoryAsync(long userId, CancellationToken cancellationToken);
}

public sealed record NewCalorieLimitEntry(
    long UserId,
    double WeightKg,
    double HeightCm,
    int Age,
    Gender Gender,
    ActivityLevel Activity,
    double WeeklyLossKg,
    int DailyCalorieLimit,
    DateTimeOffset CreatedAt);

public sealed record DailyCalorieLimitResult(
    long RecordId,
    int DailyCalorieLimit,
    DateTimeOffset CreatedAt);

public sealed record CalorieLimitHistoryItem(
    long RecordId,
    int DailyCalorieLimit,
    double WeightKg,
    double WeeklyLossKg,
    DateTimeOffset CreatedAt);
