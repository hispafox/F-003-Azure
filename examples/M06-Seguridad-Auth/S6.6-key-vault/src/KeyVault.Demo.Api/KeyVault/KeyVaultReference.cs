using System.Text.RegularExpressions;

namespace KeyVault.Demo.Api.KeyVault;

public sealed record ReferenciaKv(string Vault, string Secret, string? Version);

// Slide 6 — `@Microsoft.KeyVault(VaultName=...;SecretName=...)` en App
// Settings. Construir y parsear esa sintaxis. Lógica pura.
public static partial class KeyVaultReference
{
    [GeneratedRegex(
        @"^@Microsoft\.KeyVault\(VaultName=(?<v>[^;]+);SecretName=(?<s>[^;)]+)(;SecretVersion=(?<ver>[^;)]+))?\)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex Patron();

    public static string Construir(string vault, string secret, string? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vault);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var v = string.IsNullOrWhiteSpace(version) ? "" : $";SecretVersion={version}";
        return $"@Microsoft.KeyVault(VaultName={vault};SecretName={secret}{v})";
    }

    public static bool EsReferencia(string valor) =>
        !string.IsNullOrWhiteSpace(valor) && Patron().IsMatch(valor.Trim());

    public static ReferenciaKv Parsear(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var m = Patron().Match(valor.Trim());
        if (!m.Success)
            throw new FormatException("No es una Key Vault Reference válida");
        return new ReferenciaKv(
            m.Groups["v"].Value,
            m.Groups["s"].Value,
            m.Groups["ver"].Success ? m.Groups["ver"].Value : null);
    }
}
