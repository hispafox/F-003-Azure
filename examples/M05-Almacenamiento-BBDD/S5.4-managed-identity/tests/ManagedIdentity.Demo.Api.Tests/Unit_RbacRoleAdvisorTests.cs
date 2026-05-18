using ManagedIdentity.Demo.Api.Security;

namespace ManagedIdentity.Demo.Api.Tests;

// CAPA 1 — least privilege: rol mínimo + sufijo App Setting (slides 17/23/27).
[Trait("Category", "Unit")]
public class Unit_RbacRoleAdvisorTests
{
    [Theory]
    [InlineData(ServicioDestino.BlobStorage, Acceso.Lectura, "Storage Blob Data Reader")]
    [InlineData(ServicioDestino.BlobStorage, Acceso.LecturaEscritura, "Storage Blob Data Contributor")]
    [InlineData(ServicioDestino.BlobStorage, Acceso.Propietario, "Storage Blob Data Owner")]
    [InlineData(ServicioDestino.CosmosDb, Acceso.Lectura, "Cosmos DB Built-in Data Reader")]
    [InlineData(ServicioDestino.CosmosDb, Acceso.LecturaEscritura, "Cosmos DB Built-in Data Contributor")]
    [InlineData(ServicioDestino.KeyVault, Acceso.Lectura, "Key Vault Secrets User")]
    [InlineData(ServicioDestino.KeyVault, Acceso.Propietario, "Key Vault Secrets Officer")]
    [InlineData(ServicioDestino.ServiceBus, Acceso.Lectura, "Azure Service Bus Data Receiver")]
    [InlineData(ServicioDestino.ServiceBus, Acceso.LecturaEscritura, "Azure Service Bus Data Sender")]
    public void Recomendar_Rol_Minimo(ServicioDestino s, Acceso a, string esperado)
        => Assert.Equal(esperado, RbacRoleAdvisor.Recomendar(s, a));

    [Theory]
    [InlineData(ServicioDestino.CosmosDb, "__accountEndpoint")]
    [InlineData(ServicioDestino.BlobStorage, "__blobServiceUri")]
    [InlineData(ServicioDestino.ServiceBus, "__fullyQualifiedNamespace")]
    [InlineData(ServicioDestino.AzureSql, "Authentication=Active Directory Default")]
    public void SufijoAppSetting(ServicioDestino s, string esperado)
        => Assert.Equal(esperado, RbacRoleAdvisor.SufijoAppSetting(s));

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Contributor", true)]
    [InlineData("User Access Administrator", true)]
    [InlineData("Storage Blob Data Reader", false)]
    [InlineData("Cosmos DB Built-in Data Contributor", false)]
    public void EsRolPeligroso(string rol, bool esperado)   // slide 27 anti-pattern 3
        => Assert.Equal(esperado, RbacRoleAdvisor.EsRolPeligroso(rol));

    [Fact]
    public void Ningun_Rol_Recomendado_Es_Peligroso()
    {
        foreach (var s in Enum.GetValues<ServicioDestino>())
            foreach (var a in Enum.GetValues<Acceso>())
                Assert.False(RbacRoleAdvisor.EsRolPeligroso(
                    RbacRoleAdvisor.Recomendar(s, a).Split('+')[0].Trim()));
    }
}
