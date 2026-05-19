using KeyVault.Demo.Api.KeyVault;

namespace KeyVault.Demo.Api.Tests;

// CAPA 1 — Key Vault References en App Settings (slide 6).
[Trait("Category", "Unit")]
public class Unit_KeyVaultReferenceTests
{
    [Fact]
    public void Construir_Sin_Version()
        => Assert.Equal(
            "@Microsoft.KeyVault(VaultName=kv-prod;SecretName=StripeApiKey)",
            KeyVaultReference.Construir("kv-prod", "StripeApiKey"));

    [Fact]
    public void Construir_Con_Version()
        => Assert.Equal(
            "@Microsoft.KeyVault(VaultName=kv-prod;SecretName=ApiKey;SecretVersion=abc123)",
            KeyVaultReference.Construir("kv-prod", "ApiKey", "abc123"));

    [Fact]
    public void RoundTrip_Construir_Parsear()
    {
        var r = KeyVaultReference.Parsear(
            KeyVaultReference.Construir("kv-ventas", "SendGridApiKey"));
        Assert.Equal("kv-ventas", r.Vault);
        Assert.Equal("SendGridApiKey", r.Secret);
        Assert.Null(r.Version);
    }

    [Theory]
    [InlineData("@Microsoft.KeyVault(VaultName=v;SecretName=s)", true)]
    [InlineData("@microsoft.keyvault(VaultName=v;SecretName=s)", true)] // case-insensitive
    [InlineData("sk_live_abc123", false)]
    [InlineData("", false)]
    [InlineData("@Microsoft.KeyVault(VaultName=v)", false)]             // sin SecretName
    public void EsReferencia(string valor, bool esperado)
        => Assert.Equal(esperado, KeyVaultReference.EsReferencia(valor));

    [Fact]
    public void Parsear_Invalido_Lanza()
        => Assert.Throws<FormatException>(
            () => KeyVaultReference.Parsear("no-es-una-referencia"));
}
