using Dr.Demo.Api.Dr;

namespace Dr.Demo.Api.Tests;

// CAPA 1 — qué backup trae cada servicio (slides 3, 6, 19).
[Trait("Category", "Unit")]
public class Unit_BackupPolicyAdvisorTests
{
    [Theory]
    [InlineData(ServicioAzure.CosmosDb, true, true)]
    [InlineData(ServicioAzure.AzureSql, true, true)]
    [InlineData(ServicioAzure.BlobStorage, false, true)]   // sin auto, pero PITR con versioning
    [InlineData(ServicioAzure.TableStorage, false, false)]
    [InlineData(ServicioAzure.QueueStorage, false, false)]
    public void Describir_BackupYPitr(ServicioAzure s, bool auto, bool pitr)
    {
        var c = BackupPolicyAdvisor.Describir(s);
        Assert.Equal(auto, c.BackupAutomatico);
        Assert.Equal(pitr, c.PointInTime);
    }

    [Theory]
    // Slide 3 — lo que NO tiene backup automático y debes configurar tú.
    [InlineData(ServicioAzure.TableStorage, true)]
    [InlineData(ServicioAzure.QueueStorage, true)]
    [InlineData(ServicioAzure.AppService, true)]
    [InlineData(ServicioAzure.BlobStorage, true)]
    [InlineData(ServicioAzure.CosmosDb, false)]
    [InlineData(ServicioAzure.AzureSql, false)]
    [InlineData(ServicioAzure.KeyVault, false)]
    public void RequiereConfiguracionManual(ServicioAzure s, bool esperado)
        => Assert.Equal(esperado, BackupPolicyAdvisor.RequiereConfiguracionManual(s));

    [Fact]
    public void KeyVault_Tiene_SoftDelete()      // slide 19: 90 días
        => Assert.True(BackupPolicyAdvisor.Describir(ServicioAzure.KeyVault).SoftDelete);
}
