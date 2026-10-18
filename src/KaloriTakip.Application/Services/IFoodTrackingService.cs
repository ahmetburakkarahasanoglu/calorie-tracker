namespace KaloriTakip.Application.Services;

public interface IFoodTrackingService
{
    Task<FoodAnalysisResult> AnalyzeImageAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken);

    Task<AddFoodResult> AddFoodAsync(
        long userId,
        long nutritionId,
        decimal grams,
        DateOnly? date,
        CancellationToken cancellationToken);

    Task<DailyFoodLogSummary> GetForDateAsync(
        long userId,
        DateOnly? date,
        CancellationToken cancellationToken);

    Task<FoodLogItem?> GetEntryAsync(long userId, long entryId, CancellationToken cancellationToken);

    Task<FoodEntryDeleteResult> DeleteEntryAsync(
        long userId,
        long entryId,
        CancellationToken cancellationToken);

    Task<FoodDayCompletion?> CompleteDayAsync(
        long userId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<bool> IsDayCompletedAsync(long userId, DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<FoodDayCompletion>> GetCompletedDaysAsync(
        long userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
}

public interface IFoodImageRecognizer
{
    Task<RecognizedFood?> RecognizeAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken);
}

public interface IFoodNutritionCatalog
{
    Task<IReadOnlyList<NutritionOption>> SearchAsync(
        string foodName,
        CancellationToken cancellationToken);

    Task<NutritionOption?> GetByIdAsync(long nutritionId, CancellationToken cancellationToken);
}

public interface IFoodTrackingRepository
{
    Task<long> AddAsync(
        long userId,
        NutritionOption nutrition,
        decimal grams,
        decimal calories,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);

    Task<DailyFoodLogSummary> GetForDateAsync(
        long userId,
        DateOnly? date,
        CancellationToken cancellationToken);

    Task<FoodLogItem?> GetEntryAsync(long userId, long entryId, CancellationToken cancellationToken);

    Task<bool> DeleteEntryAsync(long userId, long entryId, CancellationToken cancellationToken);

    Task<bool> IsDayCompletedAsync(long userId, DateOnly date, CancellationToken cancellationToken);

    Task MarkDayCompletedAsync(
        long userId,
        DateOnly date,
        int? dailyCalorieLimit,
        bool? metTarget,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FoodDayCompletion>> GetCompletedDaysAsync(
        long userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);
}

public sealed record RecognizedFood(string Name, string DisplayName, decimal Confidence);

public sealed record NutritionOption(
    long NutritionId,
    string Description,
    decimal CaloriesPer100Grams,
    string DataType,
    int ServingGrams = 100);

public sealed record FoodAnalysisResult(
    RecognizedFood? Food,
    IReadOnlyList<NutritionOption> NutritionOptions,
    FoodAnalysisFailure Failure);

public enum FoodAnalysisFailure
{
    None,
    NoFoodRecognized,
    NutritionNotFound,
    DayClosed
}

public sealed record AddFoodResult(FoodLogItem? Entry, FoodAnalysisFailure Failure);

public sealed record FoodLogItem(
    long Id,
    long? NutritionId,
    string ProductName,
    decimal Grams,
    decimal Calories,
    DateTimeOffset CreatedAt);

public sealed record DailyFoodLogSummary(
    int? DailyCalorieLimit,
    decimal CaloriesConsumed,
    decimal? CaloriesRemaining,
    IReadOnlyList<FoodLogItem> Entries);

public sealed record FoodDayCompletion(DateOnly Date, bool? MetTarget);

public enum FoodEntryDeleteResult
{
    Deleted,
    NotFound,
    DayLocked
}
