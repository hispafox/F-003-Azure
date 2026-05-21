using ProyectoIntegrador.Diseno.Demo.Api.Diseno;

namespace ProyectoIntegrador.Diseno.Demo.Api.Tests;

// CAPA 1 — recomendador de bloque siguiente (slide 5).
[Trait("Category", "Unit")]
public class Unit_BloqueRecommenderTests
{
    [Fact]
    public void Sin_Bicep_Recomienda_Bloque_A()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema());
        Assert.Equal(Bloque.A_Infraestructura, r.Bloque);
        Assert.Equal("45 min", r.Duracion);
        Assert.Contains(r.Tareas, t =>
            t.Contains("main.bicep", StringComparison.Ordinal));
    }

    [Fact]
    public void Con_Bicep_Pero_Sin_Api_Recomienda_Bloque_B()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema(
            Bicep: EstadoComponente.Desplegado));
        Assert.Equal(Bloque.B_ApiYAuth, r.Bloque);
        Assert.Equal("60 min", r.Duracion);
        Assert.Contains(r.Tareas, t =>
            t.Contains("DefaultAzureCredential", StringComparison.Ordinal));
    }

    [Fact]
    public void Con_A_Y_B_Recomienda_Bloque_C()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema(
            Bicep: EstadoComponente.Desplegado,
            AppService: EstadoComponente.Desplegado,
            Cosmos: EstadoComponente.Desplegado,
            Entra: EstadoComponente.Desplegado,
            KeyVault: EstadoComponente.Desplegado,
            ManagedIdentity: EstadoComponente.Desplegado));
        Assert.Equal(Bloque.C_FunctionsYSb, r.Bloque);
        Assert.Contains(r.Tareas, t =>
            t.Contains("CosmosDBTrigger", StringComparison.Ordinal));
    }

    [Fact]
    public void Con_A_B_Y_C_Recomienda_Bloque_D()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema(
            Bicep: EstadoComponente.Desplegado,
            AppService: EstadoComponente.Desplegado,
            Cosmos: EstadoComponente.Desplegado,
            Entra: EstadoComponente.Desplegado,
            KeyVault: EstadoComponente.Desplegado,
            ManagedIdentity: EstadoComponente.Desplegado,
            Functions: EstadoComponente.Desplegado,
            ServiceBus: EstadoComponente.Desplegado));
        Assert.Equal(Bloque.D_PipelineYMonitor, r.Bloque);
        Assert.Equal("30 min", r.Duracion);
        Assert.Contains(r.Tareas, t =>
            t.Contains("smoke test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Todo_Desplegado_Devuelve_Terminado()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema(
            AppService: EstadoComponente.Desplegado,
            Functions: EstadoComponente.Desplegado,
            Cosmos: EstadoComponente.Desplegado,
            ServiceBus: EstadoComponente.Desplegado,
            Entra: EstadoComponente.Desplegado,
            KeyVault: EstadoComponente.Desplegado,
            ManagedIdentity: EstadoComponente.Desplegado,
            AppInsights: EstadoComponente.Desplegado,
            Bicep: EstadoComponente.Desplegado,
            Pipeline: EstadoComponente.Desplegado));
        Assert.Equal(Bloque.Terminado, r.Bloque);
        Assert.Contains(r.Tareas, t =>
            t.Contains("EntregaEvaluator", StringComparison.Ordinal));
    }

    [Fact]
    public void Cada_Recomendacion_Lleva_Justificacion_No_Vacia()
    {
        var r = BloqueRecommender.Recomendar(new EstadoSistema());
        Assert.False(string.IsNullOrWhiteSpace(r.Justificacion));
    }
}
