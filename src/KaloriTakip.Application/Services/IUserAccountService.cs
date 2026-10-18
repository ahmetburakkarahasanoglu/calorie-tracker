namespace KaloriTakip.Application.Services;

public interface IUserAccountService
{
    Task<UserAccountResult> RegisterAsync(string username, string password, CancellationToken cancellationToken);

    Task<UserAccountResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}

public sealed record UserAccountResult(long? UserId, string? Username, UserAccountFailure Failure)
{
    public bool Succeeded => UserId.HasValue;
}

public enum UserAccountFailure
{
    None,
    UsernameAlreadyExists,
    UsernameNotFound,
    InvalidCredentials
}
