namespace KaloriTakip.Application.Services;

public sealed class FoodTrackingService(
    IFoodImageRecognizer imageRecognizer,
    IFoodNutritionCatalog nutritionCatalog,
    IFoodTrackingRepository repository) : IFoodTrackingService
{
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<FoodAnalysisResult> AnalyzeImageAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken)
    {
        var food = await imageRecognizer.RecognizeAsync(image, contentType, cancellationToken);
        if (food is null)
        {
            return new FoodAnalysisResult(null, [], FoodAnalysisFailure.NoFoodRecognized);
        }

        var nutritionOptions = await nutritionCatalog.SearchAsync(food.Name, cancellationToken);
        return nutritionOptions.Count == 0
            ? new FoodAnalysisResult(food, [], FoodAnalysisFailure.NutritionNotFound)
            : new FoodAnalysisResult(food, nutritionOptions, FoodAnalysisFailure.None);
    }

    public async Task<AddFoodResult> AddFoodAsync(
        long userId,
        long nutritionId,
        decimal grams,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var nutrition = await nutritionCatalog.GetByIdAsync(nutritionId, cancellationToken);
        if (nutrition is null)
        {
            return new AddFoodResult(null, FoodAnalysisFailure.NutritionNotFound);
        }

        var nowInTurkey = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone);
        var targetDate = date ?? DateOnly.FromDateTime(nowInTurkey.DateTime);
        if (targetDate != DateOnly.FromDateTime(nowInTurkey.DateTime)
            || await repository.IsDayCompletedAsync(userId, targetDate, cancellationToken))
        {
            return new AddFoodResult(null, FoodAnalysisFailure.DayClosed);
        }

        var calories = decimal.Round(nutrition.CaloriesPer100Grams * grams / 100m, 2);
        var localDateTime = new DateTime(
            targetDate.Year,
            targetDate.Month,
            targetDate.Day,
            nowInTurkey.Hour,
            nowInTurkey.Minute,
            nowInTurkey.Second,
            nowInTurkey.Millisecond,
            DateTimeKind.Unspecified);
        var createdAt = new DateTimeOffset(
            localDateTime,
            TurkeyTimeZone.GetUtcOffset(localDateTime)).ToUniversalTime();
        var id = await repository.AddAsync(
            userId, nutrition, grams, calories, createdAt, cancellationToken);

        return new AddFoodResult(
            new FoodLogItem(id, nutrition.NutritionId, nutrition.Description, grams, calories, createdAt),
            FoodAnalysisFailure.None);
    }

    public Task<DailyFoodLogSummary> GetForDateAsync(
        long userId,
        DateOnly? date,
        CancellationToken cancellationToken)
        => repository.GetForDateAsync(userId, date, cancellationToken);

    public Task<FoodLogItem?> GetEntryAsync(long userId, long entryId, CancellationToken cancellationToken)
        => repository.GetEntryAsync(userId, entryId, cancellationToken);

    public async Task<FoodEntryDeleteResult> DeleteEntryAsync(
        long userId,
        long entryId,
        CancellationToken cancellationToken)
    {
        var entry = await repository.GetEntryAsync(userId, entryId, cancellationToken);
        if (entry is null) return FoodEntryDeleteResult.NotFound;

        var nowInTurkey = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone);
        var entryDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(entry.CreatedAt, TurkeyTimeZone).DateTime);
        var today = DateOnly.FromDateTime(nowInTurkey.DateTime);
        if (entryDate != today || await repository.IsDayCompletedAsync(userId, entryDate, cancellationToken))
        {
            return FoodEntryDeleteResult.DayLocked;
        }

        var deleted = await repository.DeleteEntryAsync(userId, entryId, cancellationToken);
        return deleted ? FoodEntryDeleteResult.Deleted : FoodEntryDeleteResult.NotFound;
    }

    public async Task<FoodDayCompletion?> CompleteDayAsync(
        long userId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var nowInTurkey = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone);
        if (date != DateOnly.FromDateTime(nowInTurkey.DateTime)
            || await repository.IsDayCompletedAsync(userId, date, cancellationToken))
        {
            return null;
        }

        var summary = await repository.GetForDateAsync(userId, date, cancellationToken);
        bool? metTarget = summary.DailyCalorieLimit is null
            ? null
            : summary.CaloriesRemaining >= 0;
        await repository.MarkDayCompletedAsync(
            userId,
            date,
            summary.DailyCalorieLimit,
            metTarget,
            DateTimeOffset.UtcNow,
            cancellationToken);
        return new FoodDayCompletion(date, metTarget);
    }

    public Task<bool> IsDayCompletedAsync(long userId, DateOnly date, CancellationToken cancellationToken)
        => repository.IsDayCompletedAsync(userId, date, cancellationToken);

    public Task<IReadOnlyList<FoodDayCompletion>> GetCompletedDaysAsync(
        long userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
        => repository.GetCompletedDaysAsync(userId, startDate, endDate, cancellationToken);
}
