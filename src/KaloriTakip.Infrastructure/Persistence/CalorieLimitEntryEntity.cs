namespace KaloriTakip.Infrastructure.Persistence;

public sealed class CalorieLimitEntryEntity
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public UserEntity? User { get; set; }
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public double WeeklyLossKg { get; set; }
    public int DailyCalorieLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
