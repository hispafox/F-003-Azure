namespace Practica.Demo.Api.Practica;

// Slide 7 — App Settings de la práctica: tenant/clientId en claro,
// secretos como Key Vault References (S6.6). Lógica pura: construye el
// diccionario y verifica que NO hay secretos en claro.
public static class KeyVaultRefAppSettings
{
    public static string Referencia(string vault, string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vault);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return $"@Microsoft.KeyVault(VaultName={vault};SecretName={secret})";
    }

    public static IReadOnlyDictionary<string, string> Construir(
        string tenantId, string clientId, string vault) =>
        new Dictionary<string, string>
        {
            ["AzureAd__TenantId"] = tenantId,
            ["AzureAd__ClientId"] = clientId,                       // público
            ["AzureAd__ClientSecret"] = Referencia(vault, "AzureAd-ClientSecret"),
            ["ExternalApiKey"] = Referencia(vault, "ExternalApiKey"),
        };

    // Slide 11 — entregable: cero secretos en claro (solo referencias).
    public static bool SoloReferencias(IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        foreach (var (k, v) in settings)
        {
            if (k.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                || k.Contains("ApiKey", StringComparison.OrdinalIgnoreCase))
            {
                if (!v.StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }
        return true;
    }
}
