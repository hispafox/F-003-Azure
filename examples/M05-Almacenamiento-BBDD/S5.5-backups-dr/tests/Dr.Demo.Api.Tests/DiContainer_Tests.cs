using Dr.Demo.Api.Dr;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Dr.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. No hay CAPA de integración
// (backups/DR/PITR no son emulables, requieren Azure real) → este test
// es el único que ejercita el grafo DI. Cubre la lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void DrPlanner_Se_Resuelve_Y_Genera_Plan_Coherente()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IDrPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IDrPlanner>());

        var plan = planner.Generar(
            Criticidad.Importante,
            [ServicioAzure.CosmosDb, ServicioAzure.TableStorage, ServicioAzure.AppService],
            rpoObjetivoMin: 15,
            rtoObjetivoMin: 60);

        Assert.Equal(nameof(EstrategiaDr.WarmStandby), plan.EstrategiaRecomendada);
        Assert.True(plan.CumpleObjetivos);                       // 15/60 ⊇ WarmStandby
        Assert.Equal(3, plan.Servicios.Count);
        // Table + AppService no tienen backup automático → 2 avisos.
        Assert.Equal(2, plan.Avisos.Count);
    }

    [Fact]
    public void Plan_Avisa_Cuando_La_Estrategia_No_Cumple_El_SLA()
    {
        using var factory = new WebApplicationFactory<Program>();
        var planner = factory.Services.GetRequiredService<IDrPlanner>();

        // Interno → ColdStandby, pero piden RTO 10 min: no cumple (slide 22).
        var plan = planner.Generar(
            Criticidad.Interno, [ServicioAzure.AzureSql], 5, 10);

        Assert.False(plan.CumpleObjetivos);
        Assert.Contains(plan.Avisos, a => a.Contains("no cumple"));
    }
}
