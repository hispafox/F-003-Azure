using ProyectoIntegrador.Diseno.Demo.Api.Diseno;

namespace ProyectoIntegrador.Diseno.Demo.Api.Tests;

// CAPA 1 — checklist de los 10 componentes (slide 3/4).
[Trait("Category", "Unit")]
public class Unit_ArquitecturaChecklistTests
{
    [Fact]
    public void Inventariar_Devuelve_Los_10_Componentes()
    {
        var inv = ArquitecturaChecklist.Inventariar(new EstadoSistema());
        Assert.Equal(10, inv.Count);
        Assert.Contains(inv, c => c.Componente == Componente.AppServiceApi);
        Assert.Contains(inv, c => c.Componente == Componente.AzureFunctions);
        Assert.Contains(inv, c => c.Componente == Componente.CosmosDb);
        Assert.Contains(inv, c => c.Componente == Componente.ServiceBus);
        Assert.Contains(inv, c => c.Componente == Componente.EntraIdAuth);
        Assert.Contains(inv, c => c.Componente == Componente.KeyVault);
        Assert.Contains(inv, c => c.Componente == Componente.ManagedIdentity);
        Assert.Contains(inv, c => c.Componente == Componente.ApplicationInsights);
        Assert.Contains(inv, c => c.Componente == Componente.BicepIaC);
        Assert.Contains(inv, c => c.Componente == Componente.PipelineCiCd);
    }

    [Fact]
    public void Todo_Pendiente_Da_0_Por_Ciento()
    {
        var p = ArquitecturaChecklist.PorcentajeDesplegado(new EstadoSistema());
        Assert.Equal(0, p);
    }

    [Fact]
    public void Todo_Desplegado_Da_100_Por_Ciento()
    {
        var sistema = new EstadoSistema(
            AppService: EstadoComponente.Desplegado,
            Functions: EstadoComponente.Desplegado,
            Cosmos: EstadoComponente.Desplegado,
            ServiceBus: EstadoComponente.Desplegado,
            Entra: EstadoComponente.Desplegado,
            KeyVault: EstadoComponente.Desplegado,
            ManagedIdentity: EstadoComponente.Desplegado,
            AppInsights: EstadoComponente.Desplegado,
            Bicep: EstadoComponente.Desplegado,
            Pipeline: EstadoComponente.Desplegado);
        Assert.Equal(100, ArquitecturaChecklist.PorcentajeDesplegado(sistema));
    }

    [Fact]
    public void En_Progreso_No_Cuenta_Como_Desplegado()
    {
        var sistema = new EstadoSistema(
            Bicep: EstadoComponente.EnProgreso,
            AppService: EstadoComponente.EnProgreso);
        Assert.Equal(0, ArquitecturaChecklist.PorcentajeDesplegado(sistema));
    }

    [Fact]
    public void Cinco_Desplegados_De_Diez_Da_50_Por_Ciento()
    {
        var sistema = new EstadoSistema(
            Bicep: EstadoComponente.Desplegado,
            AppService: EstadoComponente.Desplegado,
            Cosmos: EstadoComponente.Desplegado,
            ManagedIdentity: EstadoComponente.Desplegado,
            KeyVault: EstadoComponente.Desplegado);
        Assert.Equal(50, ArquitecturaChecklist.PorcentajeDesplegado(sistema));
    }

    [Fact]
    public void Cada_Componente_Lleva_Descripcion_No_Vacia()
    {
        var inv = ArquitecturaChecklist.Inventariar(new EstadoSistema());
        Assert.All(inv, c => Assert.False(string.IsNullOrWhiteSpace(c.Descripcion)));
    }
}
