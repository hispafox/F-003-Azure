using System.Text.RegularExpressions;

namespace Security.Demo.Api.Security;

public sealed record ReglaSecreto(string Id, string Descripcion, Regex Patron);
public sealed record Hallazgo(string Regla, string Fragmento);

// Slides 4, 22 — los secretos en repos/config son la causa #1 de
// brechas accidentales. Reglas tipo gitleaks como lógica pura. Una
// referencia a Key Vault NO es un secreto (el valor lo resuelve la MI).
public static class SecretScanner
{
    private const string KeyVaultRef = "@Microsoft.KeyVault(";

    public static readonly IReadOnlyList<ReglaSecreto> Reglas =
    [
        new("azure-storage-key", "Azure Storage Account Key",
            new Regex(@"AccountKey=[A-Za-z0-9+/=]{40,}", RegexOptions.Compiled)),
        new("shared-access-key", "Shared Access Key (SAS / Service Bus)",
            new Regex(@"SharedAccessKey=[^;\s]+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        new("sas-token", "SAS token (sig=)",
            new Regex(@"[?&]sig=[A-Za-z0-9%]{10,}", RegexOptions.Compiled)),
        new("password", "Password en connection string",
            new Regex(@"(?:password|pwd)\s*=\s*[^;\s""']+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        new("generic-secret", "Secret / API key",
            new Regex(@"(?:secret|api[_-]?key)\s*=\s*[^;\s""']+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    public static IReadOnlyList<Hallazgo> Escanear(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        // Slide 22 — un App Setting con referencia a Key Vault es seguro:
        // no contiene el secreto, lo resuelve App Service con su MI.
        if (contenido.Contains(KeyVaultRef, StringComparison.OrdinalIgnoreCase))
            return [];

        var hallazgos = new List<Hallazgo>();
        foreach (var r in Reglas)
        {
            var m = r.Patron.Match(contenido);
            if (m.Success)
            {
                var frag = m.Value.Length > 40 ? m.Value[..40] + "…" : m.Value;
                hallazgos.Add(new Hallazgo(r.Id, frag));
            }
        }
        return hallazgos;
    }

    public static bool TieneSecretos(string contenido) => Escanear(contenido).Count > 0;
}
