using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessManagementSystem.Api.Models;

namespace BusinessManagementSystem.Api.Security;

public sealed class TokenService
{
    private readonly byte[] _secret;

    public TokenService(IConfiguration configuration)
    {
        var secret = configuration["Auth:SigningKey"] ?? "development-only-change-this-key-before-production";
        _secret = Encoding.UTF8.GetBytes(secret);
    }

    public string Create(User user)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = user.Id,
            name = user.FullName,
            email = user.Email,
            role = user.Role,
            exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds()
        }));

        var signature = Sign($"{header}.{payload}");
        return $"{header}.{payload}.{signature}";
    }

    public AuthUser? Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3 || Sign($"{parts[0]}.{parts[1]}") != parts[2])
        {
            return null;
        }

        var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var payload = JsonSerializer.Deserialize<TokenPayload>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (payload is null || payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return null;
        }

        return new AuthUser(payload.Sub, payload.Name, payload.Email, payload.Role);
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(_secret);
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private sealed record TokenPayload(int Sub, string Name, string Email, string Role, long Exp);
}

public sealed record AuthUser(int Id, string FullName, string Email, string Role);
