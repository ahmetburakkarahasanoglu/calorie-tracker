using System.Globalization;
using KaloriTakip.Application.Services;
using Microsoft.VisualBasic.FileIO;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class CsvFoodNutritionCatalog : IFoodNutritionCatalog
{
    private readonly Dictionary<string, NutritionOption> _foodsByName;
    private readonly Dictionary<long, NutritionOption> _foodsById;

    public CsvFoodNutritionCatalog(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("The food nutrition CSV file was not found.", csvPath);
        }

        _foodsByName = new Dictionary<string, NutritionOption>(StringComparer.OrdinalIgnoreCase);
        _foodsById = [];

        using var parser = new TextFieldParser(csvPath);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields()
            ?? throw new InvalidDataException("The food nutrition CSV has no header row.");
        var classIndex = FindColumn(headers, "sinif");
        var servingDescriptionIndex = FindColumn(headers, "porsiyon_aciklama");
        var servingGramsIndex = FindColumn(headers, "porsiyon_gram");
        var servingCaloriesIndex = FindColumn(headers, "kalori_porsiyon");
        var caloriesPer100GramsIndex = FindColumn(headers, "kalori_100g");
        var lastRequiredColumn = Math.Max(
            Math.Max(classIndex, servingDescriptionIndex),
            Math.Max(Math.Max(servingGramsIndex, servingCaloriesIndex), caloriesPer100GramsIndex));

        long id = 0;
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length <= lastRequiredColumn)
            {
                throw new InvalidDataException("The food nutrition CSV contains an incomplete row.");
            }

            var name = fields[classIndex].Trim();
            if (name.Length == 0
                || !int.TryParse(fields[servingGramsIndex], CultureInfo.InvariantCulture, out var servingGrams)
                || servingGrams <= 0
                || !decimal.TryParse(fields[servingCaloriesIndex], CultureInfo.InvariantCulture, out var servingCalories)
                || !decimal.TryParse(fields[caloriesPer100GramsIndex], CultureInfo.InvariantCulture, out var caloriesPer100Grams)
                || servingCalories < 0
                || servingCalories > 10000
                || caloriesPer100Grams is < 0 or > 1000)
            {
                throw new InvalidDataException("The food nutrition CSV contains an invalid food or calorie value.");
            }

            var displayName = CultureInfo.GetCultureInfo("tr-TR")
                .TextInfo.ToTitleCase(name.Replace('-', ' '));
            var option = new NutritionOption(
                ++id,
                $"{displayName} — {fields[servingDescriptionIndex].Trim()} ({servingGrams} g)",
                caloriesPer100Grams,
                "Diyet modeli",
                servingGrams);
            if (!_foodsByName.TryAdd(name, option) || !_foodsById.TryAdd(id, option))
            {
                throw new InvalidDataException($"The food nutrition CSV contains a duplicate food name: {name}.");
            }
        }

        if (_foodsById.Count == 0)
        {
            throw new InvalidDataException("The food nutrition CSV contains no food rows.");
        }
    }

    public Task<IReadOnlyList<NutritionOption>> SearchAsync(
        string foodName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<NutritionOption> options = _foodsByName.TryGetValue(foodName, out var option)
            ? [option]
            : [];
        return Task.FromResult(options);
    }

    public Task<NutritionOption?> GetByIdAsync(long nutritionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _foodsById.TryGetValue(nutritionId, out var option);
        return Task.FromResult(option);
    }

    private static int FindColumn(string[] headers, string name)
    {
        var index = Array.FindIndex(headers, header => string.Equals(header.Trim(), name, StringComparison.Ordinal));
        return index >= 0
            ? index
            : throw new InvalidDataException($"The food nutrition CSV is missing the '{name}' column.");
    }
}
