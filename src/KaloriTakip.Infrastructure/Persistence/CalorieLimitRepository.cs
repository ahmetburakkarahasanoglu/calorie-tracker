using KaloriTakip.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class CalorieLimitRepository(KaloriTakipDbContext dbContext) : ICalorieLimitRepository
{
    public async Task<long> SaveAsync(
        NewCalorieLimitEntry entry,
        CancellationToken cancellationToken)
    {
        var entity = new CalorieLimitEntryEntity
        {
            UserId = entry.UserId,
            WeightKg = entry.WeightKg,
            HeightCm = entry.HeightCm,
            Age = entry.Age,
            Gender = entry.Gender.ToString(),
            Activity = entry.Activity.ToString(),
            WeeklyLossKg = entry.WeeklyLossKg,
            DailyCalorieLimit = entry.DailyCalorieLimit,
            CreatedAt = entry.CreatedAt
        };

        dbContext.CalorieLimitEntries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<IReadOnlyList<CalorieLimitHistoryItem>?> GetHistoryAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);
        if (!userExists)
        {
            return null;
        }

        return await dbContext.CalorieLimitEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new CalorieLimitHistoryItem(
                entry.Id,
                entry.DailyCalorieLimit,
                entry.WeightKg,
                entry.WeeklyLossKg,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
