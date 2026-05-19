using KeyVault.Demo.Api.KeyVault;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KeyVault.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Sin CAPA de integración (Key Vault
// no es emulable de forma fiable), este test es el único que ejercita
// el grafo DI. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void KeyVaultPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IKeyVaultPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IKeyVaultPlanner>());

        // API key externa → KV Secret + App Setting reference.
        var ext = planner.Planificar(
            QueGuardar.ApiKeyExterna, AccesoKv.Lectura, "kv-prod", "StripeApiKey");
        Assert.Equal(nameof(Destino.KeyVaultSecret), ext.Destino);
        Assert.True(ext.VaAKeyVault);
        Assert.Equal("Key Vault Secrets User", ext.RolMinimo);
        Assert.Equal(
            "@Microsoft.KeyVault(VaultName=kv-prod;SecretName=StripeApiKey)",
            ext.AppSettingReference);

        // Azure-a-Azure → Managed Identity, NO Key Vault.
        var azAz = planner.Planificar(
            QueGuardar.ConexionAzureAAzure, AccesoKv.Lectura, "kv-prod", "x");
        Assert.Equal(nameof(Destino.ManagedIdentity), azAz.Destino);
        Assert.False(azAz.VaAKeyVault);
        Assert.Null(azAz.AppSettingReference);
    }
}
