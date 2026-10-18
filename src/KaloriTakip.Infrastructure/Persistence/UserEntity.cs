namespace KaloriTakip.Infrastructure.Persistence;

public sealed class UserEntity
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public ICollection<CalorieLimitEntryEntity> CalorieLimitEntries { get; set; } = [];
    public ICollection<FoodLogEntryEntity> FoodLogEntries { get; set; } = [];
    public ICollection<FoodDayCompletionEntity> FoodDayCompletions { get; set; } = [];
}
