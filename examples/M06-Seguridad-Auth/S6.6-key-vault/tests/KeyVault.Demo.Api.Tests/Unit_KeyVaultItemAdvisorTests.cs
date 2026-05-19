using KeyVault.Demo.Api.KeyVault;

namespace KeyVault.Demo.Api.Tests;

// CAPA 1 — dónde va cada cosa + rol mínimo (slides 2-5).
[Trait("Category", "Unit")]
public class Unit_KeyVaultItemAdvisorTests
{
    [Theory]
    [InlineData(QueGuardar.ConexionAzureAAzure, Destino.ManagedIdentity)]
    [InlineData(QueGuardar.ApiKeyExterna, Destino.KeyVaultSecret)]
    [InlineData(QueGuardar.ClientSecretAppReg, Destino.KeyVaultSecret)]
    [InlineData(QueGuardar.CertificadoSsl, Destino.KeyVaultCertificate)]
    [InlineData(QueGuardar.ClaveCifrado, Destino.KeyVaultKey)]
    public void Donde(QueGuardar q, Destino esperado)
        => Assert.Equal(esperado, KeyVaultItemAdvisor.Donde(q));

    [Theory]
    [InlineData(Destino.KeyVaultSecret, AccesoKv.Lectura, "Key Vault Secrets User")]
    [InlineData(Destino.KeyVaultSecret, AccesoKv.Gestion, "Key Vault Secrets Officer")]
    [InlineData(Destino.KeyVaultKey, AccesoKv.UsoCripto, "Key Vault Crypto User")]
    [InlineData(Destino.KeyVaultKey, AccesoKv.Gestion, "Key Vault Crypto Officer")]
    [InlineData(Destino.KeyVaultCertificate, AccesoKv.Gestion, "Key Vault Certificates Officer")]
    public void RolMinimo(Destino d, AccesoKv a, string esperado)
        => Assert.Equal(esperado, KeyVaultItemAdvisor.RolMinimo(d, a));

    [Fact]
    public void Ningun_Rol_Recomendado_Es_Administrator()
    {
        foreach (var d in new[] { Destino.KeyVaultSecret, Destino.KeyVaultKey, Destino.KeyVaultCertificate })
            foreach (var a in Enum.GetValues<AccesoKv>())
                Assert.DoesNotContain("Administrator", KeyVaultItemAdvisor.RolMinimo(d, a));
    }

    [Fact]
    public void Rbac_Recomendado_Sobre_Access_Policies()
        => Assert.True(KeyVaultItemAdvisor.RbacRecomendadoSobreAccessPolicies);
}
