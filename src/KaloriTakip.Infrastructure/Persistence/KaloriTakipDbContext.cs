using Microsoft.EntityFrameworkCore;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class KaloriTakipDbContext(DbContextOptions<KaloriTakipDbContext> options) : DbContext(options)
{
    public DbSet<CalorieLimitEntryEntity> CalorieLimitEntries => Set<CalorieLimitEntryEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<FoodLogEntryEntity> FoodLogEntries => Set<FoodLogEntryEntity>();
    public DbSet<FoodDayCompletionEntity> FoodDayCompletions => Set<FoodDayCompletionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<CalorieLimitEntryEntity>();
        entry.ToTable("calorie_limit_entries");
        entry.HasKey(item => item.Id);
        entry.Property(item => item.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        entry.Property(item => item.WeightKg).HasColumnName("weight_kg").IsRequired();
        entry.Property(item => item.HeightCm).HasColumnName("height_cm").IsRequired();
        entry.Property(item => item.Age).HasColumnName("age").IsRequired();
        entry.Property(item => item.Gender).HasColumnName("gender").HasMaxLength(16).IsRequired();
        entry.Property(item => item.Activity).HasColumnName("activity").HasMaxLength(16).IsRequired();
        entry.Property(item => item.WeeklyLossKg).HasColumnName("weekly_loss_kg").IsRequired();
        entry.Property(item => item.DailyCalorieLimit).HasColumnName("daily_calorie_limit").IsRequired();
        entry.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        entry.Property(item => item.UserId).HasColumnName("user_id");
        entry.HasOne(item => item.User)
            .WithMany(user => user.CalorieLimitEntries)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        var user = modelBuilder.Entity<UserEntity>();
        user.ToTable("users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        user.Property(item => item.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        user.Property(item => item.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        user.Property(item => item.Email).HasColumnName("email").HasMaxLength(254);
        user.Property(item => item.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254);
        user.Property(item => item.Username).HasColumnName("username").HasMaxLength(32);
        user.Property(item => item.PasswordHash).HasColumnName("password_hash").HasMaxLength(512);
        user.HasIndex(item => item.NormalizedEmail).IsUnique();
        user.HasIndex(item => item.Username).IsUnique();

        var foodEntry = modelBuilder.Entity<FoodLogEntryEntity>();
        foodEntry.ToTable("food_log_entries");
        foodEntry.HasKey(item => item.Id);
        foodEntry.Property(item => item.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        foodEntry.Property(item => item.UserId).HasColumnName("user_id").IsRequired();
        foodEntry.Property(item => item.NutritionId).HasColumnName("nutrition_id");
        foodEntry.Property(item => item.ProductName).HasColumnName("product_name").HasMaxLength(300).IsRequired();
        foodEntry.Property(item => item.Grams).HasColumnName("grams").HasPrecision(8, 2).IsRequired();
        foodEntry.Property(item => item.CaloriesPer100Grams).HasColumnName("calories_per_100g").HasPrecision(8, 2).IsRequired();
        foodEntry.Property(item => item.Calories).HasColumnName("calories").HasPrecision(8, 2).IsRequired();
        foodEntry.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        foodEntry.HasOne(item => item.User)
            .WithMany(userAccount => userAccount.FoodLogEntries)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        foodEntry.HasIndex(item => new { item.UserId, item.CreatedAt });

        var dayCompletion = modelBuilder.Entity<FoodDayCompletionEntity>();
        dayCompletion.ToTable("food_day_completions");
        dayCompletion.HasKey(item => new { item.UserId, item.Date });
        dayCompletion.Property(item => item.UserId).HasColumnName("user_id");
        dayCompletion.Property(item => item.Date).HasColumnName("date").HasColumnType("date");
        dayCompletion.Property(item => item.DailyCalorieLimit).HasColumnName("daily_calorie_limit");
        dayCompletion.Property(item => item.MetTarget).HasColumnName("met_target");
        dayCompletion.Property(item => item.CompletedAt).HasColumnName("completed_at").IsRequired();
        dayCompletion.HasOne(item => item.User)
            .WithMany(userAccount => userAccount.FoodDayCompletions)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
