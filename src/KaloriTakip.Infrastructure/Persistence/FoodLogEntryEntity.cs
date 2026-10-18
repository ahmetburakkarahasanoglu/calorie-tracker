namespace KaloriTakip.Infrastructure.Persistence;

public sealed class FoodLogEntryEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public long? NutritionId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Grams { get; set; }
    public decimal CaloriesPer100Grams { get; set; }
    public decimal Calories { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
