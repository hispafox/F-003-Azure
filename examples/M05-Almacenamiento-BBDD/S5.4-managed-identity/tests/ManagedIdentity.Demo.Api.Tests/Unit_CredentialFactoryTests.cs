using ManagedIdentity.Demo.Api.Security;
using Microsoft.Extensions.Configuration;

namespace ManagedIdentity.Demo.Api.Tests;

// CAPA 1 — config → DefaultAzureCredentialOptions (slides 16, 22, 25).
[Trait("Category", "Unit")]
public class Unit_CredentialFactoryTests
{
    private static IConfiguration Cfg(Dictionary<string, string?> d)
        => new ConfigurationBuilder().AddInMemoryCollection(d).Build();

    [Fact]
    public void Sin_Config_Opciones_Por_Defecto()
    {
        var o = CredentialFactory.CrearOpciones(Cfg([]));
        Assert.Null(o.ManagedIdentityClientId);
        Assert.Null(o.TenantId);
        Assert.False(o.ExcludeManagedIdentityCredential);
    }

    [Fact]
    public void UserAssignedClientId_Se_Aplica()    // slide 22
    {
        var o = CredentialFactory.CrearOpciones(Cfg(new()
        {
            [CredentialFactory.KeyUserAssignedClientId] = "uami-client-123",
        }));
        Assert.Equal("uami-client-123", o.ManagedIdentityClientId);
    }

    [Fact]
    public void TenantId_Se_Aplica()                // slide 25 (cross-tenant)
    {
        var o = CredentialFactory.CrearOpciones(Cfg(new()
        {
            [CredentialFactory.KeyTenantId] = "tenant-abc",
        }));
        Assert.Equal("tenant-abc", o.TenantId);
    }

    [Fact]
    public void LocalDev_Excluye_ManagedIdentity()  // slide 16 (acelera local)
    {
        var o = CredentialFactory.CrearOpciones(Cfg(new()
        {
            [CredentialFactory.KeyLocalDev] = "true",
        }));
        Assert.True(o.ExcludeManagedIdentityCredential);
    }

    [Fact]
    public void Crear_Devuelve_Una_TokenCredential()
        => Assert.NotNull(CredentialFactory.Crear(Cfg([])));
}
