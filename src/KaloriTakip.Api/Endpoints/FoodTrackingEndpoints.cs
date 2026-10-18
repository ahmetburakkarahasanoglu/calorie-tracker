using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KaloriTakip.Api.Contracts;
using KaloriTakip.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KaloriTakip.Api.Endpoints;

public static class FoodTrackingEndpoints
{
    private const long MaximumImageSizeBytes = 5 * 1024 * 1024;
    private const long MaximumRequestSizeBytes = MaximumImageSizeBytes + 64 * 1024;
    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static WebApplication MapFoodTrackingEndpoints(this WebApplication app)
    {
        app.MapPost("/api/food-analysis", AnalyzeFoodImage)
            .WithName("AnalyzeFoodImage")
            .WithSummary("Fotoğraftaki yiyeceği tanır ve besin seçeneklerini bulur")
            .WithDescription("Yerel modeliyle görseli analiz eder ve yerel kalori tablosundan besin bilgisi döndürür. Fotoğraf saklanmaz.")
            .WithMetadata(new RequestSizeLimitAttribute(MaximumRequestSizeBytes))
            .Produces<FoodAnalysisResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status502BadGateway)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization()
            .RequireRateLimiting("food-analysis");

        app.MapPost("/api/food-entries", AddFoodEntry)
            .WithName("AddFoodEntry")
            .WithSummary("Onaylanan yiyeceği gram miktarıyla günlük kayda ekler")
            .Produces<FoodLogEntryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();

        app.MapGet("/api/food-entries/today", GetToday)
            .WithName("GetTodayFoodLog")
            .WithSummary("Seçilen günün tüketim toplamını ve günlük limiti getirir")
            .Produces<TodayFoodLogResponse>()
            .RequireAuthorization();

        app.MapGet("/api/food-entries/{entryId:long}", GetEntry)
            .WithName("GetFoodEntry")
            .WithSummary("Kullanıcının yiyecek kaydını getirir")
            .Produces<FoodLogEntryResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapDelete("/api/food-entries/{entryId:long}", DeleteEntry)
            .WithName("DeleteFoodEntry")
            .WithSummary("Kullanıcının yiyecek kaydını siler")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapGet("/api/food-days/completions", GetCompletedDays)
            .WithName("GetCompletedFoodDays")
            .WithSummary("Seçilen ayda tamamlanan günleri ve hedef durumlarını getirir")
            .Produces<IReadOnlyList<CompletedFoodDayResponse>>()
            .RequireAuthorization();

        app.MapPost("/api/food-days/{date}/complete", CompleteDay)
            .WithName("CompleteFoodDay")
            .WithSummary("Günü tamamlar ve hedef durumunu takvime kaydeder")
            .Produces<CompleteFoodDayResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> AnalyzeFoodImage(
        HttpRequest request,
        ClaimsPrincipal principal,
        IFoodTrackingService foodTrackingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaximumRequestSizeBytes)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Fotoğraf çok büyük",
                detail: "En fazla 5 MB boyutunda bir fotoğraf yükleyebilirsin.");
        }

        if (!request.HasFormContentType)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Fotoğraf gerekli",
                Detail = "Bir yiyecek fotoğrafı seçip tekrar dene."
            });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var imageFile = form.Files.GetFile("image");
        if (imageFile is null || imageFile.Length == 0)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Fotoğraf gerekli",
                Detail = "Bir yiyecek fotoğrafı seçip tekrar dene."
            });
        }

        if (imageFile.Length > MaximumImageSizeBytes)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Fotoğraf çok büyük",
                detail: "En fazla 5 MB boyutunda bir fotoğraf yükleyebilirsin.");
        }

        var contentType = imageFile.ContentType.ToLowerInvariant();
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Fotoğraf biçimi desteklenmiyor",
                Detail = "JPG, PNG veya WebP fotoğrafı yükle."
            });
        }

        await using var imageStream = imageFile.OpenReadStream();
        using var memoryStream = new MemoryStream((int)imageFile.Length);
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        if (!HasExpectedImageSignature(imageBytes, contentType))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Fotoğraf okunamadı",
                Detail = "JPG, PNG veya WebP biçiminde geçerli bir fotoğraf yükle."
            });
        }

        var logger = loggerFactory.CreateLogger("FoodTrackingEndpoints");
        FoodAnalysisResult result;
        try
        {
            result = await foodTrackingService.AnalyzeImageAsync(
                imageBytes, contentType, cancellationToken);
        }
        catch (FoodProviderNotConfiguredException exception)
        {
            logger.LogWarning(
                "Food image analysis cannot run because {Provider} is not configured.",
                exception.Provider);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Diyet modeli yapılandırılmamış",
                detail: "Yerel diyet model servisini ve kalori tablosu yolunu yapılandırıp yeniden başlat.");
        }
        catch (FoodProviderRequestException exception)
        {
            logger.LogWarning(exception, "Food image analysis provider request failed.");
            if (exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (exception.RetryAfter is { } retryAfter)
                {
                    request.HttpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                return TypedResults.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Görsel analiz kotasına ulaşıldı",
                    detail: "Görsel analiz servisi isteği geçici olarak reddetti. Biraz bekleyip tekrar dene.");
            }

            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Yiyecek analizi yapılamadı",
                detail: "Yerel model servisine veya kalori tablosuna erişilemedi. Biraz sonra tekrar dene.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Food analysis provider is unreachable.");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Yiyecek analizi yapılamadı",
                detail: "Yerel model servisine veya kalori tablosuna erişilemedi. Model servisini kontrol edip tekrar dene.");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Food analysis provider timed out.");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Yiyecek analizi zaman aşımına uğradı",
                detail: "Yerel model servisi zamanında yanıt vermedi. Biraz sonra tekrar dene.");
        }

        if (result.Failure == FoodAnalysisFailure.NoFoodRecognized)
        {
            return TypedResults.UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Yiyecek tanınamadı",
                Detail = "Fotoğrafta belirgin bir yiyecek bulunamadı. Daha net ve yakından bir fotoğraf çek."
            });
        }

        if (result.Failure == FoodAnalysisFailure.NutritionNotFound)
        {
            return TypedResults.UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Besin bilgisi bulunamadı",
                Detail = "Yiyecek tanındı ancak kalori bilgisi bulunamadı. Farklı bir fotoğraf deneyebilirsin."
            });
        }

        return TypedResults.Ok(new FoodAnalysisResponse(
            result.Food!.DisplayName,
            result.Food.Name,
            result.Food.Confidence,
            result.NutritionOptions.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> AddFoodEntry(
        AddFoodEntryRequest request,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        IFoodTrackingService foodTrackingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Error: result.ErrorMessage ?? "Geçersiz değer.")))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Error).ToArray());
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        AddFoodResult result;
        var logger = loggerFactory.CreateLogger("FoodTrackingEndpoints");
        try
        {
            result = await foodTrackingService.AddFoodAsync(
                userId, request.NutritionId, request.Grams, request.Date, cancellationToken);
        }
        catch (FoodProviderNotConfiguredException exception)
        {
            logger.LogWarning(
                "Food logging cannot run because {Provider} is not configured.",
                exception.Provider);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Diyet modeli yapılandırılmamış",
                detail: "Modelin kalori tablosunu yapılandırıp API'yi yeniden başlat.");
        }
        catch (FoodProviderRequestException exception)
        {
            logger.LogWarning(exception, "Local food nutrition data could not be read while logging a food.");
            if (exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (exception.RetryAfter is { } retryAfter)
                {
                    httpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                return TypedResults.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Besin servisi geçici olarak kullanılamıyor",
                    detail: "Biraz bekleyip tekrar dene.");
            }

            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Besin bilgisi alınamadı",
                detail: "Kalori tablosuna erişilemedi. Dosya yolunu kontrol edip tekrar dene.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Food nutrition provider is unreachable while logging a food.");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Besin bilgisi alınamadı",
                detail: "Kalori tablosuna erişilemedi. Dosya yolunu kontrol edip tekrar dene.");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Food nutrition provider timed out while logging a food.");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Besin bilgisi zaman aşımına uğradı",
                detail: "Kalori tablosu isteği zaman aşımına uğradı. Biraz sonra tekrar dene.");
        }

        if (result.Entry is null)
        {
            if (result.Failure == FoodAnalysisFailure.DayClosed)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Bu gün kayıt almıyor",
                    Detail = "Gelecek veya tamamlanmış bir güne yiyecek ekleyemezsin."
                });
            }

            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Besin kaydı bulunamadı",
                Detail = "Seçilen besin bilgisini artık bulamıyorum. Fotoğrafı tekrar analiz et."
            });
        }

        return TypedResults.CreatedAtRoute(
            ToResponse(result.Entry),
            "GetFoodEntry",
            new { entryId = result.Entry.Id });
    }

    private static async Task<Ok<TodayFoodLogResponse>> GetToday(
        ClaimsPrincipal principal,
        DateOnly? date,
        IFoodTrackingService foodTrackingService,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var summary = await foodTrackingService.GetForDateAsync(userId, date, cancellationToken);

        return TypedResults.Ok(new TodayFoodLogResponse(
            summary.DailyCalorieLimit,
            summary.CaloriesConsumed,
            summary.CaloriesRemaining,
            summary.Entries.Select(ToResponse).ToArray()));
    }

    private static async Task<Results<Ok<FoodLogEntryResponse>, NotFound>> GetEntry(
        long entryId,
        ClaimsPrincipal principal,
        IFoodTrackingService foodTrackingService,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var entry = await foodTrackingService.GetEntryAsync(userId, entryId, cancellationToken);
        return entry is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(entry));
    }

    private static async Task<IResult> DeleteEntry(
        long entryId,
        ClaimsPrincipal principal,
        IFoodTrackingService foodTrackingService,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await foodTrackingService.DeleteEntryAsync(userId, entryId, cancellationToken);
        return result switch
        {
            FoodEntryDeleteResult.Deleted => TypedResults.NoContent(),
            FoodEntryDeleteResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Bu günün kayıtları değiştirilemiyor",
                Detail = "Yalnızca bugünün tamamlanmamış kayıtlarını silebilirsin."
            })
        };
    }

    private static async Task<IResult> GetCompletedDays(
        int year,
        int month,
        ClaimsPrincipal principal,
        IFoodTrackingService foodTrackingService,
        CancellationToken cancellationToken)
    {
        if (year is < 1 or > 9998 || month is < 1 or > 12)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["month"] = ["Geçerli bir yıl ve ay seç."]
            });
        }

        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var days = await foodTrackingService.GetCompletedDaysAsync(
            userId, startDate, endDate, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<CompletedFoodDayResponse>>(
            days.Select(day => new CompletedFoodDayResponse(day.Date, day.MetTarget)).ToArray());
    }

    private static async Task<IResult> CompleteDay(
        DateOnly date,
        ClaimsPrincipal principal,
        IFoodTrackingService foodTrackingService,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TurkeyTimeZone).DateTime);
        if (date != today)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["date"] = ["Yalnızca bugünün kaydını tamamlayabilirsin."]
            });
        }

        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (await foodTrackingService.IsDayCompletedAsync(userId, date, cancellationToken))
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Gün zaten tamamlanmış",
                Detail = "Tamamlanan gün takvimde kayıtlı."
            });
        }

        var completion = await foodTrackingService.CompleteDayAsync(userId, date, cancellationToken);
        if (completion is null)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Gün tamamlanamadı",
                Detail = "Bugün zaten tamamlanmış veya artık geçerli gün değil."
            });
        }

        return TypedResults.Ok(new CompleteFoodDayResponse(completion.Date, completion.MetTarget));
    }

    private static bool HasExpectedImageSignature(byte[] image, string contentType)
        => contentType switch
        {
            "image/jpeg" => image.Length >= 3
                && image[0] == 0xFF && image[1] == 0xD8 && image[2] == 0xFF,
            "image/png" => image.Length >= 8
                && image.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => image.Length >= 12
                && image.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && image.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };

    private static NutritionOptionResponse ToResponse(NutritionOption option)
        => new(
            option.NutritionId,
            option.Description,
            option.CaloriesPer100Grams,
            option.DataType,
            option.ServingGrams);

    private static FoodLogEntryResponse ToResponse(FoodLogItem entry)
        => new(entry.Id, entry.ProductName, entry.Grams, entry.Calories, entry.CreatedAt);
}
