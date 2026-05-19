namespace Datos.Demo.Api.Datos;

// Slides 3, 5, 14 — cifrado en tránsito: TLS 1.2 mínimo y connection
// strings que fuerzan el canal cifrado. Lógica pura.
public static class TlsTransitValidator
{
    // Slide 3 — TLS 1.0 y 1.1 están DEPRECADOS; mínimo 1.2.
    public static bool VersionPermitida(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var v = version.Trim().TrimStart('v', 'V')
            .Replace("TLS", "", StringComparison.OrdinalIgnoreCase)
            .Replace("_", ".").Trim();
        return decimal.TryParse(v, System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture, out var n)
               && n >= 1.2m;
    }

    // Slide 5/14 — Azure SQL debe ir con Encrypt=true.
    public static bool SqlCifradoEnTransito(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var cs = connectionString;
        return cs.Contains("Encrypt=true", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("Encrypt=Mandatory", StringComparison.OrdinalIgnoreCase)
            || cs.Contains("Encrypt=Strict", StringComparison.OrdinalIgnoreCase);
    }

    // Slide 5/14 — Storage debe ir por HTTPS.
    public static bool StorageCifradoEnTransito(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var cs = connectionString;
        // Endpoint https explícito, o solo la URL https (Managed Identity).
        return cs.Contains("DefaultEndpointsProtocol=https",
                   StringComparison.OrdinalIgnoreCase)
            || cs.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
