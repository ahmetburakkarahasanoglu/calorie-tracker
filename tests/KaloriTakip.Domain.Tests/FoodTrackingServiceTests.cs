using KaloriTakip.Application.Services;

namespace KaloriTakip.Domain.Tests;

public class FoodTrackingServiceTests
{
    [Fact]
    public async Task AnalyzeImageAsync_ReturnsRecognizedFoodAndNutritionMatches()
    {
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(
            new RecognizedFood("banana", "Muz", 0.95m),
            [nutrition],
            nutrition);

        var result = await service.AnalyzeImageAsync([1, 2, 3], "image/jpeg", CancellationToken.None);

        Assert.Equal("Muz", result.Food?.DisplayName);
        Assert.Equal(FoodAnalysisFailure.None, result.Failure);
        Assert.Equal([nutrition], result.NutritionOptions);
    }

    [Fact]
    public async Task AnalyzeImageAsync_ReturnsFailureWhenNoFoodIsRecognized()
    {
        var service = CreateService(null, [], null);

        var result = await service.AnalyzeImageAsync([1], "image/jpeg", CancellationToken.None);

        Assert.Null(result.Food);
        Assert.Equal(FoodAnalysisFailure.NoFoodRecognized, result.Failure);
    }

    [Fact]
    public async Task AddFoodAsync_CalculatesCaloriesFromNutritionAndGrams()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(new RecognizedFood("banana", "Muz", 0.95m), [nutrition], nutrition, repository);

        var result = await service.AddFoodAsync(42, nutrition.NutritionId, 118m, null, CancellationToken.None);

        Assert.NotNull(result.Entry);
        Assert.Equal(105.02m, result.Entry.Calories);
        Assert.Equal(42, repository.UserId);
        Assert.Equal(118m, repository.Grams);
        Assert.True(repository.WasSaved);
    }

    [Fact]
    public async Task AddFoodAsync_DoesNotSaveWhenNutritionSourceDoesNotExist()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var service = CreateService(new RecognizedFood("banana", "Muz", 0.95m), [], null, repository);

        var result = await service.AddFoodAsync(42, 123L, 100m, null, CancellationToken.None);

        Assert.Null(result.Entry);
        Assert.Equal(FoodAnalysisFailure.NutritionNotFound, result.Failure);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task AddFoodAsync_UsesSelectedCalendarDate()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(new RecognizedFood("banana", "Muz", 0.95m), [nutrition], nutrition, repository);
        var selectedDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        await service.AddFoodAsync(42, nutrition.NutritionId, 100m, selectedDate, CancellationToken.None);

        Assert.Equal(selectedDate, DateOnly.FromDateTime(repository.CreatedAt!.Value.UtcDateTime.AddHours(3)));
    }

    [Fact]
    public async Task AddFoodAsync_DoesNotAddToCompletedDay()
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
        var repository = new InMemoryFoodTrackingRepository
        {
            CompletedDate = today
        };
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(null, [nutrition], nutrition, repository);

        var result = await service.AddFoodAsync(
            42, nutrition.NutritionId, 100m, repository.CompletedDate, CancellationToken.None);

        Assert.Null(result.Entry);
        Assert.Equal(FoodAnalysisFailure.DayClosed, result.Failure);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task AddFoodAsync_DoesNotAddToPastDay()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(null, [nutrition], nutrition, repository);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var result = await service.AddFoodAsync(
            42, nutrition.NutritionId, 100m, today.AddDays(-1), CancellationToken.None);

        Assert.Null(result.Entry);
        Assert.Equal(FoodAnalysisFailure.DayClosed, result.Failure);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task AddFoodAsync_DoesNotAddToFutureDay()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var nutrition = new NutritionOption(173944, "Banana, raw", 89m, "Foundation");
        var service = CreateService(null, [nutrition], nutrition, repository);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var result = await service.AddFoodAsync(
            42, nutrition.NutritionId, 100m, today.AddDays(1), CancellationToken.None);

        Assert.Null(result.Entry);
        Assert.Equal(FoodAnalysisFailure.DayClosed, result.Failure);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task CompleteDayAsync_SavesWhetherTargetWasMet()
    {
        var repository = new InMemoryFoodTrackingRepository
        {
            Summary = new DailyFoodLogSummary(200, 150m, 50m, [])
        };
        var service = CreateService(null, [], null, repository);
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var completion = await service.CompleteDayAsync(42, date, CancellationToken.None);

        Assert.NotNull(completion);
        Assert.Equal(date, completion!.Date);
        Assert.True(completion.MetTarget);
        Assert.Equal(date, repository.CompletedDate);
        Assert.Equal(200, repository.CompletedCalorieLimit);
        Assert.True(repository.CompletedMetTarget);
    }

    [Fact]
    public async Task CompleteDayAsync_RecordsWhenTargetWasExceeded()
    {
        var repository = new InMemoryFoodTrackingRepository
        {
            Summary = new DailyFoodLogSummary(200, 250m, -50m, [])
        };
        var service = CreateService(null, [], null, repository);
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var completion = await service.CompleteDayAsync(42, date, CancellationToken.None);

        Assert.NotNull(completion);
        Assert.False(completion!.MetTarget);
        Assert.False(repository.CompletedMetTarget);
    }

    [Fact]
    public async Task CompleteDayAsync_DoesNotCompletePastDay()
    {
        var repository = new InMemoryFoodTrackingRepository();
        var service = CreateService(null, [], null, repository);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);

        var completion = await service.CompleteDayAsync(42, today.AddDays(-1), CancellationToken.None);

        Assert.Null(completion);
        Assert.Null(repository.CompletedDate);
    }

    [Fact]
    public async Task DeleteEntryAsync_DoesNotDeletePastDayEntry()
    {
        var repository = new InMemoryFoodTrackingRepository
        {
            Entry = new FoodLogItem(
                1,
                173944,
                "Banana",
                100m,
                89m,
                DateTimeOffset.UtcNow.AddDays(-1))
        };
        var service = CreateService(null, [], null, repository);

        var result = await service.DeleteEntryAsync(42, 1, CancellationToken.None);

        Assert.Equal(FoodEntryDeleteResult.DayLocked, result);
        Assert.False(repository.WasDeleted);
    }

    private static FoodTrackingService CreateService(
        RecognizedFood? recognizedFood,
        IReadOnlyList<NutritionOption> searchResults,
        NutritionOption? selectedNutrition,
        InMemoryFoodTrackingRepository? repository = null)
        => new(
            new StubImageRecognizer(recognizedFood),
            new StubNutritionCatalog(searchResults, selectedNutrition),
            repository ?? new InMemoryFoodTrackingRepository());

    private sealed class StubImageRecognizer(RecognizedFood? result) : IFoodImageRecognizer
    {
        public Task<RecognizedFood?> RecognizeAsync(
            byte[] image,
            string contentType,
            CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class StubNutritionCatalog(
        IReadOnlyList<NutritionOption> searchResults,
        NutritionOption? selectedNutrition) : IFoodNutritionCatalog
    {
        public Task<IReadOnlyList<NutritionOption>> SearchAsync(string foodName, CancellationToken cancellationToken)
            => Task.FromResult(searchResults);

        public Task<NutritionOption?> GetByIdAsync(long nutritionId, CancellationToken cancellationToken)
            => Task.FromResult(selectedNutrition?.NutritionId == nutritionId ? selectedNutrition : null);
    }

    private sealed class InMemoryFoodTrackingRepository : IFoodTrackingRepository
    {
        public long? UserId { get; private set; }
        public decimal? Grams { get; private set; }
        public bool WasSaved { get; private set; }
        public DateTimeOffset? CreatedAt { get; private set; }
        public DateOnly? CompletedDate { get; set; }
        public int? CompletedCalorieLimit { get; private set; }
        public bool? CompletedMetTarget { get; private set; }
        public DailyFoodLogSummary Summary { get; set; } = new(null, 0, null, []);
        public FoodLogItem? Entry { get; set; }
        public bool WasDeleted { get; private set; }

        public Task<long> AddAsync(
            long userId,
            NutritionOption nutrition,
            decimal grams,
            decimal calories,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            UserId = userId;
            Grams = grams;
            CreatedAt = createdAt;
            WasSaved = true;
            return Task.FromResult(1L);
        }

        public Task<DailyFoodLogSummary> GetForDateAsync(
            long userId,
            DateOnly? date,
            CancellationToken cancellationToken)
            => Task.FromResult(Summary);

        public Task<FoodLogItem?> GetEntryAsync(long userId, long entryId, CancellationToken cancellationToken)
            => Task.FromResult(Entry?.Id == entryId ? Entry : null);

        public Task<bool> DeleteEntryAsync(long userId, long entryId, CancellationToken cancellationToken)
        {
            WasDeleted = true;
            return Task.FromResult(Entry?.Id == entryId);
        }

        public Task<bool> IsDayCompletedAsync(long userId, DateOnly date, CancellationToken cancellationToken)
            => Task.FromResult(CompletedDate == date);

        public Task MarkDayCompletedAsync(
            long userId,
            DateOnly date,
            int? dailyCalorieLimit,
            bool? metTarget,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            CompletedDate = date;
            CompletedCalorieLimit = dailyCalorieLimit;
            CompletedMetTarget = metTarget;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FoodDayCompletion>> GetCompletedDaysAsync(
            long userId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FoodDayCompletion>>([]);
    }
}
