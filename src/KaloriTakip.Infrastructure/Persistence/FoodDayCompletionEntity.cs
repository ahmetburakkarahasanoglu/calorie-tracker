namespace KaloriTakip.Infrastructure.Persistence;

public sealed class FoodDayCompletionEntity
{
    public long UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public int? DailyCalorieLimit { get; set; }
    public bool? MetTarget { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
