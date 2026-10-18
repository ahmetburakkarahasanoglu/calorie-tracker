using System.Net;
using System.Text;
using KaloriTakip.Application.Services;
using KaloriTakip.Infrastructure.Persistence;

namespace KaloriTakip.Domain.Tests;

public sealed class LocalViTFoodProviderTests
{
    [Fact]
    public async Task Recognizer_ParsesHighConfidencePrediction()
    {
        var recognizer = new LocalViTFoodImageRecognizer(
            CreateHttpClient("""{"label":"mercimek-corbasi","score":0.91}"""));

        var food = await recognizer.RecognizeAsync([1, 2], "image/jpeg", CancellationToken.None);

        Assert.NotNull(food);
        Assert.Equal("mercimek-corbasi", food.Name);
        Assert.Equal("Mercimek Corbasi", food.DisplayName);
        Assert.Equal(0.91m, food.Confidence);
    }

    [Fact]
    public async Task Recognizer_ReturnsNullBelowConfidenceThreshold()
    {
        var recognizer = new LocalViTFoodImageRecognizer(
            CreateHttpClient("""{"label":"mercimek-corbasi","score":0.59}"""));

        var food = await recognizer.RecognizeAsync([1], "image/jpeg", CancellationToken.None);

        Assert.Null(food);
    }

    [Fact]
    public async Task NutritionCatalog_LoadsServingAndCaloriesFromCsv()
    {
        var csvPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(
            csvPath,
            "sinif,porsiyon_aciklama,porsiyon_gram,kalori_porsiyon,kalori_100g\nmercimek-corbasi,1 Kase,200,150,75\n",
            Encoding.UTF8);

        try
        {
            var catalog = new CsvFoodNutritionCatalog(csvPath);
            var options = await catalog.SearchAsync("mercimek-corbasi", CancellationToken.None);

            Assert.Single(options);
            Assert.Equal("Mercimek Corbasi — 1 Kase (200 g)", options[0].Description);
            Assert.Equal(75m, options[0].CaloriesPer100Grams);
            Assert.Equal("Diyet modeli", options[0].DataType);
            Assert.Equal(200, options[0].ServingGrams);
            Assert.Equal(options[0], await catalog.GetByIdAsync(options[0].NutritionId, CancellationToken.None));
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static HttpClient CreateHttpClient(string json)
        => new(new StubHttpMessageHandler(json))
        {
            BaseAddress = new Uri("http://localhost/")
        };

    private sealed class StubHttpMessageHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
