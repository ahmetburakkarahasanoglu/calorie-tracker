using KaloriTakip.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class FoodTrackingRepository(KaloriTakipDbContext dbContext) : IFoodTrackingRepository
{
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<long> AddAsync(
        long userId,
        NutritionOption nutrition,
        decimal grams,
        decimal calories,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var entity = new FoodLogEntryEntity
        {
            UserId = userId,
            NutritionId = nutrition.NutritionId,
            ProductName = nutrition.Description,
            Grams = grams,
            CaloriesPer100Grams = nutrition.CaloriesPer100Grams,
            Calories = calories,
            CreatedAt = createdAt
        };

        dbContext.FoodLogEntries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<DailyFoodLogSummary> GetForDateAsync(
        long userId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone);
        var targetDate = date ?? DateOnly.FromDateTime(now.DateTime);
        var localMidnight = new DateTime(
            targetDate.Year,
            targetDate.Month,
            targetDate.Day,
            0,
            0,
            0,
            DateTimeKind.Unspecified);
        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, TurkeyTimeZone));
        var endLocalMidnight = localMidnight.AddDays(1);
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocalMidnight, TurkeyTimeZone));

        var entries = await dbContext.FoodLogEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId
                && entry.CreatedAt >= startUtc
                && entry.CreatedAt < endUtc)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new FoodLogItem(
                entry.Id,
                entry.NutritionId,
                entry.ProductName,
                entry.Grams,
                entry.Calories,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);

        var limit = await dbContext.CalorieLimitEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => (int?)entry.DailyCalorieLimit)
            .FirstOrDefaultAsync(cancellationToken);

        var consumed = entries.Sum(entry => entry.Calories);
        return new DailyFoodLogSummary(
            limit,
            consumed,
            limit is null ? null : limit.Value - consumed,
            entries);
    }

    public async Task<bool> DeleteEntryAsync(
        long userId,
        long entryId,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.FoodLogEntries
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.Id == entryId,
                cancellationToken);
        if (entry is null) return false;

        var localDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(entry.CreatedAt, TurkeyTimeZone).DateTime);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.FoodLogEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        var completion = await dbContext.FoodDayCompletions.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Date == localDate,
            cancellationToken);
        if (completion is not null)
        {
            var summary = await GetForDateAsync(userId, localDate, cancellationToken);
            completion.MetTarget = completion.DailyCalorieLimit is null
                ? null
                : completion.DailyCalorieLimit.Value >= summary.CaloriesConsumed;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<bool> IsDayCompletedAsync(long userId, DateOnly date, CancellationToken cancellationToken)
        => dbContext.FoodDayCompletions
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId && item.Date == date, cancellationToken);

    public async Task MarkDayCompletedAsync(
        long userId,
        DateOnly date,
        int? dailyCalorieLimit,
        bool? metTarget,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var completion = await dbContext.FoodDayCompletions.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Date == date,
            cancellationToken);
        if (completion is null)
        {
            completion = new FoodDayCompletionEntity
            {
                UserId = userId,
                Date = date,
                DailyCalorieLimit = dailyCalorieLimit,
                MetTarget = metTarget,
                CompletedAt = completedAt
            };
            dbContext.FoodDayCompletions.Add(completion);
        }
        else
        {
            completion.DailyCalorieLimit = dailyCalorieLimit;
            completion.MetTarget = metTarget;
            completion.CompletedAt = completedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FoodDayCompletion>> GetCompletedDaysAsync(
        long userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
        => await dbContext.FoodDayCompletions
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Date >= startDate && item.Date <= endDate)
            .OrderBy(item => item.Date)
            .Select(item => new FoodDayCompletion(item.Date, item.MetTarget))
            .ToListAsync(cancellationToken);

    public async Task<FoodLogItem?> GetEntryAsync(
        long userId,
        long entryId,
        CancellationToken cancellationToken)
        => await dbContext.FoodLogEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Id == entryId)
            .Select(entry => new FoodLogItem(
                entry.Id,
                entry.NutritionId,
                entry.ProductName,
                entry.Grams,
                entry.Calories,
                entry.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}
