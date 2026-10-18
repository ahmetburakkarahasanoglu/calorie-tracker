using System.ComponentModel.DataAnnotations;

namespace KaloriTakip.Api.Contracts;

/// <summary>Username and password for creating an account or signing in.</summary>
public sealed record AccountCredentialsRequest
{
    [Required, StringLength(32, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-32 karakter olmalıdır.")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Kullanıcı adı yalnızca İngilizce harf, rakam, nokta, tire ve alt çizgi içerebilir.")]
    public required string Username { get; init; }

    [Required, StringLength(128, MinimumLength = 12, ErrorMessage = "Şifre en az 12, en fazla 128 karakter olmalıdır.")]
    public required string Password { get; init; }
}

/// <summary>Authenticated user's public account profile.</summary>
public sealed record AccountSessionResponse(long UserId, string Username);
