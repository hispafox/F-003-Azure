using System.Text;
using System.Text.Json;

namespace Entra.Demo.Api.Entra;

public sealed record ClaimsResumen(
    string? Sub,
    string? Name,
    string? PreferredUsername,
    string? Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Groups,
    string? Aud,
    string? Iss,
    DateTimeOffset? Exp,
    bool Expirado);

// Slide 18 — los tokens JWT que recibe tu app. Esto SOLO DECODIFICA el
// payload (base64url → JSON) para inspección/didáctica: NO valida la
// firma. La validación real la hace Microsoft.Identity.Web (slide 18:
// "tu app NUNCA valida tokens manualmente"). Lógica pura, sin red.
public static class JwtInspector
{
    public static ClaimsResumen Inspeccionar(string jwt, DateTimeOffset? ahora = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwt);
        var partes = jwt.Split('.');
        if (partes.Length is not (2 or 3))
            throw new FormatException("JWT debe tener formato header.payload[.signature]");

        using var doc = JsonDocument.Parse(DecodeBase64Url(partes[1]));
        var root = doc.RootElement;

        string? S(string n) => root.TryGetProperty(n, out var v)
            && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        IReadOnlyList<string> Arr(string n)
        {
            if (!root.TryGetProperty(n, out var v)) return [];
            return v.ValueKind switch
            {
                JsonValueKind.Array => [.. v.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)],
                JsonValueKind.String => [v.GetString()!],
                _ => [],
            };
        }

        DateTimeOffset? exp = root.TryGetProperty("exp", out var e)
            && e.TryGetInt64(out var secs)
            ? DateTimeOffset.FromUnixTimeSeconds(secs)
            : null;

        var reloj = ahora ?? DateTimeOffset.UtcNow;
        var expirado = exp is not null && exp.Value < reloj;

        return new ClaimsResumen(
            S("sub"), S("name"), S("preferred_username"), S("email"),
            Arr("roles"), Arr("groups"), S("aud"), S("iss"), exp, expirado);
    }

    private static byte[] DecodeBase64Url(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b.PadRight(b.Length + (4 - b.Length % 4) % 4, '='));
    }
}
