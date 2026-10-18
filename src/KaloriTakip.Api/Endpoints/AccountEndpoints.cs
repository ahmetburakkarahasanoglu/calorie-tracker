using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KaloriTakip.Api.Contracts;
using KaloriTakip.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace KaloriTakip.Api.Endpoints;

public static class AccountEndpoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", Register)
            .WithName("RegisterAccount")
            .WithSummary("Yeni kullanıcı hesabı oluşturur")
            .Produces<AccountSessionResponse>()
            .ProducesValidationProblem()
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .AllowAnonymous()
            .RequireRateLimiting("account");

        auth.MapPost("/login", (Delegate)Login)
            .WithName("Login")
            .WithSummary("Kullanıcı adı ve şifreyle oturum açar")
            .Produces<AccountSessionResponse>()
            .ProducesValidationProblem()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .RequireRateLimiting("account");

        auth.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Oturum açmış kullanıcı profilini getirir")
            .Produces<AccountSessionResponse>()
            .RequireAuthorization();

        auth.MapPost("/logout", (Delegate)Logout)
            .WithName("Logout")
            .WithSummary("Oturumu kapatır")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<AccountSessionResponse>, ValidationProblem, Conflict<ProblemDetails>>> Register(
        AccountCredentialsRequest request,
        IUserAccountService accountService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await accountService.RegisterAsync(
            request.Username, request.Password, cancellationToken);

        if (result.Failure == UserAccountFailure.UsernameAlreadyExists)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Kullanıcı adı kullanımda",
                Detail = "Bu kullanıcı adıyla zaten hesap var."
            });
        }

        await SignInAsync(httpContext, result);
        return TypedResults.Ok(new AccountSessionResponse(result.UserId!.Value, result.Username!));
    }

    private static async Task<Results<Ok<AccountSessionResponse>, ValidationProblem, UnauthorizedHttpResult, NotFound<ProblemDetails>>> Login(
        AccountCredentialsRequest request,
        IUserAccountService accountService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var result = await accountService.AuthenticateAsync(
            request.Username, request.Password, cancellationToken);
        if (result.Failure == UserAccountFailure.UsernameNotFound)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Kullanıcı bulunamadı",
                Detail = "Bu kullanıcı adıyla kayıtlı hesap yok. Devam etmek için önce hesap oluştur."
            });
        }

        if (!result.Succeeded)
        {
            return TypedResults.Unauthorized();
        }

        await SignInAsync(httpContext, result);
        return TypedResults.Ok(new AccountSessionResponse(result.UserId!.Value, result.Username!));
    }

    private static Ok<AccountSessionResponse> GetCurrentUser(ClaimsPrincipal principal)
    {
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var username = principal.FindFirstValue(ClaimTypes.Name)!;
        return TypedResults.Ok(new AccountSessionResponse(userId, username));
    }

    private static async Task<NoContent> Logout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task SignInAsync(HttpContext httpContext, UserAccountResult account)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.UserId!.Value.ToString()),
            new Claim(ClaimTypes.Name, account.Username!)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    private static Dictionary<string, string[]> Validate(AccountCredentialsRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => (Member: member, Error: result.ErrorMessage ?? "Geçersiz değer.")))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Error).ToArray());
    }
}
