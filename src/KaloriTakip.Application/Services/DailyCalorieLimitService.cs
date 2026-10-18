using KaloriTakip.Domain;

namespace KaloriTakip.Application.Services;

public sealed class DailyCalorieLimitService(ICalorieLimitRepository repository) : IDailyCalorieLimitService
{
    public async Task<DailyCalorieLimitResult> CalculateAndSaveAsync(
        long userId,
        double weightKg,
        double heightCm,
        int age,
        Gender gender,
        ActivityLevel activity,
        double weeklyLossKg,
        CancellationToken cancellationToken)
    {
        var dailyCalorieLimit = CalorieCalculator.CalculateDailyLimit(
            weightKg, heightCm, age, gender, activity, weeklyLossKg);
        var createdAt = DateTimeOffset.UtcNow;
        var entry = new NewCalorieLimitEntry(
            userId, weightKg, heightCm, age, gender, activity, weeklyLossKg, dailyCalorieLimit, createdAt);
        var recordId = await repository.SaveAsync(entry, cancellationToken);

        return new DailyCalorieLimitResult(recordId, dailyCalorieLimit, createdAt);
    }

    public Task<IReadOnlyList<CalorieLimitHistoryItem>?> GetHistoryAsync(
        long userId,
        CancellationToken cancellationToken)
        => repository.GetHistoryAsync(userId, cancellationToken);
}
