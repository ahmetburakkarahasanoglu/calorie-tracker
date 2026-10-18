using System.ComponentModel.DataAnnotations;

namespace KaloriTakip.Api.Contracts;

/// <summary>The user's confirmed local nutrition record and serving size.</summary>
public sealed record AddFoodEntryRequest
{
    [Range(1, long.MaxValue)]
    public required long NutritionId { get; init; }

    [Range(typeof(decimal), "0.1", "10000")]
    public required decimal Grams { get; init; }

    public DateOnly? Date { get; init; }
}

/// <summary>A nutrition table entry for the food identified in the image.</summary>
public sealed record NutritionOptionResponse(
    long NutritionId,
    string Description,
    decimal CaloriesPer100Grams,
    string DataType,
    int ServingGrams);

/// <summary>The food identified by the image model and candidate nutrition data.</summary>
public sealed record FoodAnalysisResponse(
    string DisplayName,
    string FoodName,
    decimal Confidence,
    IReadOnlyList<NutritionOptionResponse> NutritionOptions);

/// <summary>A food item recorded in the user's diary.</summary>
public sealed record FoodLogEntryResponse(
    long Id,
    string ProductName,
    decimal Grams,
    decimal Calories,
    DateTimeOffset CreatedAt);

/// <summary>Today's food total, current daily goal, remaining calories and logged items.</summary>
public sealed record TodayFoodLogResponse(
    int? DailyCalorieLimit,
    decimal CaloriesConsumed,
    decimal? CaloriesRemaining,
    IReadOnlyList<FoodLogEntryResponse> Entries);

public sealed record CompleteFoodDayResponse(DateOnly Date, bool? MetTarget);

public sealed record CompletedFoodDayResponse(DateOnly Date, bool? MetTarget);
