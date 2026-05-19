using System.Security.Cryptography;
using System.Text;

namespace Oauth.Demo.Api.Oauth;

public sealed record PkcePar(string CodeVerifier, string CodeChallenge, string Method);

// Slide 6 — PKCE (Proof Key for Code Exchange, RFC 7636). El cliente
// genera un `code_verifier` aleatorio, manda su hash (`code_challenge`)
// en /authorize y el verifier en /token: previene el robo del `code`.
// Cálculo puro y determinista (testeable con los vectores del RFC).
public static class PkceGenerator
{
    public const string Method = "S256";

    // RFC 7636 §4.1: 43-128 chars del set unreserved [A-Za-z0-9-._~].
    public static string GenerarVerifier(int bytes = 32)
    {
        if (bytes is < 32 or > 96)
            throw new ArgumentOutOfRangeException(nameof(bytes));
        return Base64Url(RandomNumberGenerator.GetBytes(bytes));
    }

    // code_challenge = BASE64URL(SHA256(ASCII(code_verifier))).
    public static string Challenge(string verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);
        if (verifier.Length is < 43 or > 128)
            throw new ArgumentException("code_verifier debe tener 43-128 chars (RFC 7636)");
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    public static PkcePar Generar()
    {
        var v = GenerarVerifier();
        return new PkcePar(v, Challenge(v), Method);
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
