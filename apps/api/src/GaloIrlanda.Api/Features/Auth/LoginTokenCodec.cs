using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>Tokens opacos (link mágico e sessão): 32 bytes aleatórios; no banco, só o SHA-256.</summary>
public static class LoginTokenCodec
{
    private const int TokenBytes = 32;

    public static (string Token, byte[] Hash) Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
        return (WebEncoders.Base64UrlEncode(bytes), SHA256.HashData(bytes));
    }

    public static bool TryHash(string token, out byte[] hash)
    {
        hash = [];
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(token);
            if (bytes.Length != TokenBytes) return false;
            hash = SHA256.HashData(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
