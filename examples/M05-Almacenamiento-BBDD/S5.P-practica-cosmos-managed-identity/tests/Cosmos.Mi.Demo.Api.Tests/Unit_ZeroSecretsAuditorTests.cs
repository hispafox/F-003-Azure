using Cosmos.Mi.Demo.Api.Security;

namespace Cosmos.Mi.Demo.Api.Tests;

// CAPA 1 — el entregable de la práctica: zero secrets (slides 13, 19).
[Trait("Category", "Unit")]
public class Unit_ZeroSecretsAuditorTests
{
    [Theory]
    [InlineData("https://cuenta.documents.azure.com:443/", false)]   // solo URL
    [InlineData("AccountEndpoint=https://x;AccountKey=abc==;", true)] // ¡key!
    [InlineData("Server=x;Password=secreto;", true)]
    [InlineData("@Microsoft.KeyVault(VaultName=kv;SecretName=s)", false)] // referencia
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TieneSecreto(string? valor, bool esperado)
        => Assert.Equal(esperado, ZeroSecretsAuditor.TieneSecreto(valor));

    [Fact]
    public void Auditar_TodoLimpio_Cuando_Solo_Endpoint()
    {
        var r = ZeroSecretsAuditor.Auditar(new Dictionary<string, string?>
        {
            ["CosmosEndpoint"] = "https://cuenta.documents.azure.com:443/",
            ["Azure:TenantId"] = "tenant-abc",
        });
        Assert.True(r.TodoLimpio);
        Assert.All(r.Entradas, e => Assert.False(e.TieneSecreto));
    }

    [Fact]
    public void Auditar_Detecta_La_Entrada_Con_Key()
    {
        var r = ZeroSecretsAuditor.Auditar(new Dictionary<string, string?>
        {
            ["CosmosEndpoint"] = "https://cuenta.documents.azure.com:443/",
            ["CosmosConnection"] = "AccountEndpoint=https://x;AccountKey=zzz==;",
        });
        Assert.False(r.TodoLimpio);
        Assert.Contains(r.Entradas, e => e.Clave == "CosmosConnection" && e.TieneSecreto);
    }

    [Fact]
    public void Auditar_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => ZeroSecretsAuditor.Auditar(null!));
}
