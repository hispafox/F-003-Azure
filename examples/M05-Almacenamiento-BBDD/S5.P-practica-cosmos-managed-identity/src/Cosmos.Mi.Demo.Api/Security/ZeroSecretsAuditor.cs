namespace Cosmos.Mi.Demo.Api.Security;

public sealed record EntradaAuditada(string Clave, bool TieneSecreto);
public sealed record ResultadoAuditoria(bool TodoLimpio, IReadOnlyList<EntradaAuditada> Entradas);

// Slides 13, 19 — el entregable de la práctica es ZERO secrets. Esta
// función pura audita los App Settings: solo debería verse
// `CosmosEndpoint` (una URL, NO un secreto). Reutiliza la idea del
// ConnectionSecretScanner de S5.4.
public static class ZeroSecretsAuditor
{
    private static readonly string[] Indicadores =
    [
        "password=", "pwd=", "accountkey=", "sharedaccesskey=",
        "accesskey=", "sig=", "secret=",
    ];

    private const string KeyVaultRef = "@Microsoft.KeyVault(";

    public static bool TieneSecreto(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return false;
        // Una Key Vault Reference NO es el secreto (lo resuelve la MI).
        if (valor.Contains(KeyVaultRef, StringComparison.OrdinalIgnoreCase)) return false;
        return Indicadores.Any(i => valor.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    public static ResultadoAuditoria Auditar(IEnumerable<KeyValuePair<string, string?>> appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        var entradas = appSettings
            .Select(kv => new EntradaAuditada(kv.Key, TieneSecreto(kv.Value)))
            .ToArray();
        return new ResultadoAuditoria(entradas.All(e => !e.TieneSecreto), entradas);
    }
}
