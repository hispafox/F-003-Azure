using ManagedIdentity.Demo.Api.Security;

namespace ManagedIdentity.Demo.Api.Tests;

// CAPA 1 — detectar secretos en config (slides 2, 10, 13).
[Trait("Category", "Unit")]
public class Unit_ConnectionSecretScannerTests
{
    [Theory]
    [InlineData("Server=tcp:x;Database=d;User ID=sa;Password=Secreto123;")]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=x;AccountKey=abc==;")]
    [InlineData("Endpoint=sb://x;SharedAccessKeyName=root;SharedAccessKey=zzz=")]
    [InlineData("BlobEndpoint=https://x?sv=2022&sig=AbCdEf%3D")]
    public void Detecta_Secreto(string cs)
        => Assert.True(ConnectionSecretScanner.Escanear(cs).TieneSecreto);

    [Theory]
    // Entra ID (Azure SQL) — sin password.
    [InlineData("Server=tcp:x,1433;Database=d;Authentication=Active Directory Default;Encrypt=true;")]
    // Cosmos solo endpoint (la credencial va aparte).
    [InlineData("AccountEndpoint=https://acct.documents.azure.com:443/")]
    // Blob solo URL.
    [InlineData("https://acct.blob.core.windows.net")]
    public void No_Detecta_Secreto_Si_Es_MI(string cs)
    {
        var r = ConnectionSecretScanner.Escanear(cs);
        Assert.False(r.TieneSecreto);
        Assert.True(ConnectionSecretScanner.EsSinSecreto(cs));
    }

    [Fact]
    public void KeyVaultReference_No_Es_Secreto_Expuesto()   // slide 10
    {
        var r = ConnectionSecretScanner.Escanear(
            "@Microsoft.KeyVault(VaultName=kv-prod;SecretName=api-key)");
        Assert.False(r.TieneSecreto);
        Assert.True(r.EsKeyVaultReference);
    }

    [Fact]
    public void Lista_Indicadores_Encontrados()
    {
        var r = ConnectionSecretScanner.Escanear("...;Password=x;AccountKey=y;");
        Assert.Contains("password=", r.IndicadoresEncontrados);
        Assert.Contains("accountkey=", r.IndicadoresEncontrados);
    }

    [Fact]
    public void Null_Lanza()
        => Assert.Throws<ArgumentNullException>(
            () => ConnectionSecretScanner.Escanear(null!));
}
