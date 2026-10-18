using System.Security.Cryptography;
using KaloriTakip.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KaloriTakip.Infrastructure.Persistence;

public sealed class UserAccountService : IUserAccountService
{
    private static readonly UserEntity DummyUser = new();
    private static readonly string DummyPasswordHash = new PasswordHasher<UserEntity>()
        .HashPassword(DummyUser, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    private readonly KaloriTakipDbContext _dbContext;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;

    public UserAccountService(
        KaloriTakipDbContext dbContext,
        IPasswordHasher<UserEntity> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserAccountResult> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (await _dbContext.Users.AnyAsync(user => user.Username == normalizedUsername, cancellationToken))
        {
            return new UserAccountResult(null, null, UserAccountFailure.UsernameAlreadyExists);
        }

        var user = new UserEntity
        {
            Username = normalizedUsername,
            FirstName = string.Empty,
            LastName = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new UserAccountResult(null, null, UserAccountFailure.UsernameAlreadyExists);
        }

        return new UserAccountResult(user.Id, NormalizeUsername(username), UserAccountFailure.None);
    }

    public async Task<UserAccountResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(username);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Username == normalizedUsername, cancellationToken);

        if (user?.PasswordHash is null)
        {
            _passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, password);
            return new UserAccountResult(null, null, UserAccountFailure.UsernameNotFound);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new UserAccountResult(null, null, UserAccountFailure.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UserAccountResult(user.Id, NormalizeUsername(username), UserAccountFailure.None);
    }

    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}
