using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KaloriTakip.Application.Services;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class LocalViTFoodImageRecognizer(HttpClient httpClient) : IFoodImageRecognizer
{
    private const decimal MinimumConfidence = 0.60m;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RecognizedFood?> RecognizeAsync(
        byte[] image,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new ByteArrayContent(image);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await httpClient.PostAsync("predict", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new FoodProviderRequestException(
                "Diyet model servisi",
                $"HTTP {(int)response.StatusCode}",
                response.StatusCode,
                response.Headers.RetryAfter?.Delta);
        }

        var prediction = await response.Content.ReadFromJsonAsync<Prediction>(
            JsonOptions, cancellationToken);
        if (prediction is null
            || string.IsNullOrWhiteSpace(prediction.Label)
            || !double.IsFinite(prediction.Score)
            || prediction.Score is < 0 or > 1)
        {
            throw new FoodProviderRequestException(
                "Diyet model servisi",
                "The response did not contain a valid prediction.");
        }

        if (prediction.Score < (double)MinimumConfidence)
        {
            return null;
        }

        var foodName = prediction.Label.Trim();
        var displayName = System.Globalization.CultureInfo.GetCultureInfo("tr-TR")
            .TextInfo.ToTitleCase(foodName.Replace('-', ' '));
        return new RecognizedFood(foodName, displayName, (decimal)prediction.Score);
    }

    private sealed record Prediction(string Label, double Score);
}
