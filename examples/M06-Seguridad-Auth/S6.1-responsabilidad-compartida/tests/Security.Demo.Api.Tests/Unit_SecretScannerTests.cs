using Security.Demo.Api.Security;

namespace Security.Demo.Api.Tests;

// CAPA 1 — detección de secretos tipo gitleaks (slides 4, 22).
[Trait("Category", "Unit")]
public class Unit_SecretScannerTests
{
    [Theory]
    [InlineData("AccountName=x;AccountKey=abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJ==;")]
    [InlineData("Endpoint=sb://x;SharedAccessKey=zzz9zzz")]
    [InlineData("Server=x;User Id=sa;Password=Secreto123;")]
    [InlineData("https://x.blob.core.windows.net/c?sv=2022&sig=AbCdEf012345%3D")]
    [InlineData("ApiKey=sk_live_abcdef0123456789")]
    public void Detecta_Secretos(string contenido)
        => Assert.True(SecretScanner.TieneSecretos(contenido));

    [Theory]
    [InlineData("@Microsoft.KeyVault(VaultName=kv;SecretName=s)")]            // slide 22
    [InlineData("Server=tcp:x;Authentication=Active Directory Default;Encrypt=true;")]
    [InlineData("https://cuenta.documents.azure.com:443/")]
    [InlineData("")]
    public void No_Detecta_Cuando_No_Hay_Secreto(string contenido)
        => Assert.False(SecretScanner.TieneSecretos(contenido));

    [Fact]
    public void Escanear_Devuelve_La_Regla_Que_Disparo()
    {
        var h = SecretScanner.Escanear(
            "DefaultEndpointsProtocol=https;AccountKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefgh==;");
        Assert.Contains(h, x => x.Regla == "azure-storage-key");
    }

    [Fact]
    public void Escanear_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => SecretScanner.Escanear(null!));

    [Fact]
    public void Hay_Reglas_Definidas()
        => Assert.NotEmpty(SecretScanner.Reglas);
}
