using System.ComponentModel.DataAnnotations;
using KaloriTakip.Api.Contracts;
using KaloriTakip.Application.Services;
using KaloriTakip.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace KaloriTakip.Api.Endpoints;

public static class DailyCalorieEndpoints
{
    public static WebApplication MapDailyCalorieEndpoints(this WebApplication app)
    {
        app.MapPost("/api/calorie-limit", CalculateDailyCalorieLimit)
            .WithName("CalculateDailyCalorieLimit")
            .WithSummary("Kişisel günlük kalori limitini hesaplar")
            .WithDescription("Yetişkin kullanıcı bilgileri ve haftalık kilo verme hedefiyle günlük kalori hedefi döndürür.")
            .Produces<DailyCalorieLimitResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        app.MapGet("/api/users/me/calorie-limits", GetUserCalorieLimitHistory)
            .WithName("GetUserCalorieLimitHistory")
            .WithSummary("Kullanıcının hesaplama geçmişini getirir")
            .Produces<IReadOnlyList<CalorieLimitHistoryResponse>>()
            .RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<DailyCalorieLimitResponse>, ValidationProblem>> CalculateDailyCalorieLimit(
        CalculateDailyCalorieLimitRequest request,
        IDailyCalorieLimitService calorieLimitService,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Error: result.ErrorMessage ?? "Geçersiz değer.")))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Error).ToArray());

        if (!Enum.IsDefined(request.Gender))
        {
            errors[nameof(request.Gender)] = ["Geçerli bir cinsiyet seçin."];
        }

        if (!Enum.IsDefined(request.Activity))
        {
            errors[nameof(request.Activity)] = ["Geçerli bir aktivite seviyesi seçin."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await calorieLimitService.CalculateAndSaveAsync(
            userId,
            request.WeightKg,
            request.HeightCm,
            request.Age,
            request.Gender,
            request.Activity,
            request.WeeklyLossKg,
            cancellationToken);

        return TypedResults.Ok(new DailyCalorieLimitResponse(
            userId, result.RecordId, result.DailyCalorieLimit, result.CreatedAt));
    }

    private static async Task<Results<Ok<IReadOnlyList<CalorieLimitHistoryResponse>>, NotFound>> GetUserCalorieLimitHistory(
        ClaimsPrincipal principal,
        IDailyCalorieLimitService calorieLimitService,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var history = await calorieLimitService.GetHistoryAsync(userId, cancellationToken);
        if (history is null)
        {
            return TypedResults.NotFound();
        }

        IReadOnlyList<CalorieLimitHistoryResponse> response = history
            .Select(item => new CalorieLimitHistoryResponse(
                item.RecordId,
                item.DailyCalorieLimit,
                item.WeightKg,
                item.WeeklyLossKg,
                item.CreatedAt))
            .ToArray();

        return TypedResults.Ok(response);
    }
}
