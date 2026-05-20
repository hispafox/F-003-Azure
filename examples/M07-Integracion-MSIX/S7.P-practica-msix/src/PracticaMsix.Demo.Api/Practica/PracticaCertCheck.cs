namespace PracticaMsix.Demo.Api.Practica;

public sealed record ResultadoCheck(bool Ok, string Razon);

// Slide 7 — el error #1 de la práctica: el Subject del certificado
// (CN=...) DEBE coincidir EXACTAMENTE con el Publisher del manifest.
// Si no coincide, Windows rechaza el paquete con "package signature
// hash validation failed". Lógica pura.
public static class PracticaCertCheck
{
    // Compara el Publisher del manifest (p.ej. "CN=MsixDemoCurso") con
    // el Subject del certificado (p.ej. "CN=MsixDemoCurso"). El match
    // tiene que ser ordinal y completo — Windows no normaliza espacios.
    public static ResultadoCheck PublisherCoincide(
        string publisherManifest, string subjectCertificado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publisherManifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectCertificado);

        if (!publisherManifest.StartsWith("CN=", StringComparison.Ordinal))
            return new(false,
                $"Publisher '{publisherManifest}' no empieza por 'CN=' (slide 6).");

        if (!subjectCertificado.StartsWith("CN=", StringComparison.Ordinal))
            return new(false,
                $"Subject del cert '{subjectCertificado}' no empieza por 'CN=' (slide 7).");

        return string.Equals(publisherManifest, subjectCertificado, StringComparison.Ordinal)
            ? new(true, "Publisher del manifest coincide con el Subject del certificado.")
            : new(false,
                $"Publisher '{publisherManifest}' ≠ Subject '{subjectCertificado}'. " +
                "Windows rechazará el .msix (slide 7).");
    }

    // Slide 7 — el cert self-signed debe poder firmar código (Extended
    // Key Usage 1.3.6.1.5.5.7.3.3 = Code Signing).
    public const string OidCodeSigning = "1.3.6.1.5.5.7.3.3";

    public static ResultadoCheck UsoCorrecto(IReadOnlyList<string> ekus)
    {
        ArgumentNullException.ThrowIfNull(ekus);
        return ekus.Contains(OidCodeSigning, StringComparer.Ordinal)
            ? new(true, "EKU Code Signing presente (slide 7).")
            : new(false,
                $"Falta el EKU '{OidCodeSigning}' (Code Signing) — añade " +
                "-TextExtension al New-SelfSignedCertificate (slide 7).");
    }
}
