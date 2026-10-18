using KaloriTakip.Application.Services;
using KaloriTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KaloriTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string postgresConnectionString,
        string modelServiceUrl,
        string nutritionCsvPath)
    {
        services.AddDbContext<KaloriTakipDbContext>(options => options.UseNpgsql(postgresConnectionString));
        services.AddScoped<ICalorieLimitRepository, CalorieLimitRepository>();
        services.AddScoped<IFoodTrackingRepository, FoodTrackingRepository>();
        services.AddScoped<IFoodTrackingService, FoodTrackingService>();
        if (!Uri.TryCreate(modelServiceUrl, UriKind.Absolute, out var modelServiceUri)
            || modelServiceUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("FoodRecognition:ModelServiceUrl must be an absolute HTTP(S) URL.");
        }

        services.AddSingleton<IFoodNutritionCatalog>(new CsvFoodNutritionCatalog(nutritionCsvPath));
        services.AddSingleton(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress = modelServiceUri,
                Timeout = TimeSpan.FromSeconds(60)
            };
            return new ModelServiceHttpClient(client);
        });
        services.AddScoped<IFoodImageRecognizer>(provider =>
            new LocalViTFoodImageRecognizer(provider.GetRequiredService<ModelServiceHttpClient>().Client));
        services.AddScoped<IUserAccountService>(provider =>
            new UserAccountService(
                provider.GetRequiredService<KaloriTakipDbContext>(),
                new PasswordHasher<UserEntity>()));

        return services;
    }

    public sealed record ModelServiceHttpClient(HttpClient Client);
}
