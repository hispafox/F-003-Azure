using System.Text.RegularExpressions;

namespace AppService.Demo.Api.Telemetry;

// Slide 25 — Redacta datos personales antes de loggearlos / enviarlos
// como telemetría. Usa regex compilados para no pagar penalty por petición.
public static partial class PiiScrubber
{
    [GeneratedRegex(@"\b[\w\.\-]+@[\w\.\-]+\.\w+\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b\d{4}[\s\-]?\d{4}[\s\-]?\d{4}[\s\-]?\d{4}\b")]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b(?:Bearer\s+)?[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-+/=]{10,}\b")]
    private static partial Regex JwtRegex();

    public const string EmailPlaceholder = "[REDACTED:EMAIL]";
    public const string CreditCardPlaceholder = "[REDACTED:CC]";
    public const string TokenPlaceholder = "[REDACTED:TOKEN]";

    public static string Scrub(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Orden importante: JWT primero (puede contener letras tipo email);
        // luego tarjetas (numéricas), luego emails (genérico).
        var result = JwtRegex().Replace(input, TokenPlaceholder);
        result = CreditCardRegex().Replace(result, CreditCardPlaceholder);
        result = EmailRegex().Replace(result, EmailPlaceholder);
        return result;
    }
}
