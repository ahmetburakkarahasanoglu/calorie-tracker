using System.Net;

namespace KaloriTakip.Application.Services;

public sealed class FoodProviderNotConfiguredException(string provider)
    : InvalidOperationException($"{provider} integration is not configured.")
{
    public string Provider { get; } = provider;
}

public sealed class FoodProviderRequestException(
    string provider,
    string message,
    HttpStatusCode? statusCode = null,
    TimeSpan? retryAfter = null)
    : Exception($"{provider} request failed: {message}")
{
    public string Provider { get; } = provider;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
