namespace ManagedIdentity.Demo.Api.Security;

public sealed record ResultadoEscaneo(
    bool TieneSecreto,
    bool EsKeyVaultReference,
    IReadOnlyList<string> IndicadoresEncontrados);

// Slides 2, 10, 13 — el problema del submódulo: las connection strings
// llevan secretos (password, key, SAS) que acaban en repos/App Settings.
// Esta función pura detecta si un valor de configuración expone un
// secreto, si es seguro (endpoint + MI / Entra ID) o si es una
// referencia a Key Vault (slide 10: el App Setting NO es el secreto).
public static class ConnectionSecretScanner
{
    // Indicadores de secreto embebido (case-insensitive).
    private static readonly string[] Secretos =
    [
        "password=", "pwd=", "accountkey=", "sharedaccesskey=",
        "accesskey=", "sharedaccesskeyname=", "sig=", "secret=",
    ];

    // Marcadores de "sin secreto" (MI / Entra ID) — solo informativo.
    private const string KeyVaultRef = "@Microsoft.KeyVault(";

    public static ResultadoEscaneo Escanear(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);

        // Slide 10 — el valor es una referencia a Key Vault: el secreto
        // real lo resuelve App Service con su MI, NO está aquí.
        if (valor.Contains(KeyVaultRef, StringComparison.OrdinalIgnoreCase))
            return new ResultadoEscaneo(false, true, []);

        var encontrados = Secretos
            .Where(s => valor.Contains(s, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return new ResultadoEscaneo(encontrados.Length > 0, false, encontrados);
    }

    // Slide 13 (checklist) — ¿es una conexión sin secreto? (MI / Entra ID
    // / referencia a Key Vault / solo endpoint).
    public static bool EsSinSecreto(string valor) => !Escanear(valor).TieneSecreto;
}
