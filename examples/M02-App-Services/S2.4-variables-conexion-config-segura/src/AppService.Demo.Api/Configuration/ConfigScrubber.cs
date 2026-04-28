namespace AppService.Demo.Api.Configuration;

// Slide 28 — Scrubbing por NOMBRE de clave: si la clave parece sensible
// (password, key, secret, token, connection-string...) reemplazamos el valor
// por "***REDACTED***". Más seguro que intentar identificar valores
// sensibles por contenido (regex sobre la cadena).
public static class ConfigScrubber
{
    private static readonly string[] SensitiveTokens =
    {
        "password",
        "secret",
        "key",
        "token",
        "connectionstring",
        "credential"
    };

    public const string RedactedValue = "***REDACTED***";

    public static string Scrub(string keyName, string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return IsSensitive(keyName) ? RedactedValue : value;
    }

    public static bool IsSensitive(string keyName) =>
        SensitiveTokens.Any(token =>
            keyName.Contains(token, StringComparison.OrdinalIgnoreCase));

    public static IDictionary<string, string> ScrubAll(IConfiguration config)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in config.AsEnumerable())
        {
            if (string.IsNullOrEmpty(kv.Key)) continue;
            result[kv.Key] = Scrub(kv.Key, kv.Value);
        }
        return result;
    }
}
