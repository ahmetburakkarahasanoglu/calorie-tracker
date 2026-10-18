using KaloriTakip.Domain;

namespace KaloriTakip.Application.Services;

public interface IDailyCalorieLimitService
{
    Task<DailyCalorieLimitResult> CalculateAndSaveAsync(
        long userId,
        double weightKg,
        double heightCm,
        int age,
        Gender gender,
        ActivityLevel activity,
        double weeklyLossKg,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CalorieLimitHistoryItem>?> GetHistoryAsync(
        long userId,
        CancellationToken cancellationToken);
}
