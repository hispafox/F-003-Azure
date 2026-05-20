using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Practica.Pipeline.Demo.Api.Pipeline;

namespace Practica.Pipeline.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PracticaPipelinePlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaPipelinePlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaPipelinePlanner>());

        var plan = planner.Planificar(
            preflight: new EscenarioPreflight(
                TieneOrgADO: true, TieneRepoConPushAccess: true,
                TieneSuscripcionAzure: true, EsAdminProyectoADO: true,
                EsOwnerOUserAccessAdmin: true, PlanS1OSuperior: true,
                SlotStagingExiste: true, TieneServiceConnectionOidc: true,
                TieneAppRegistration: true, TieneAzCliInstalado: true),
            opciones: new OpcionesPipeline(
                AutoRollbackEnFallo: true, NotificarTeamsEnFallo: true),
            simulacionSmoke: new MedidasSmoke(
                HttpCode: 200, LatenciaMediaSegundos: 0.4,
                ErrorRatePorcentaje: 0.1));

        Assert.True(plan.Preflight.ListoParaArrancar);
        Assert.NotEmpty(plan.Pipeline.Etapas);
        Assert.NotNull(plan.SmokeTest);
        Assert.Equal(DecisionSmoke.Continuar, plan.SmokeTest!.Decision);
        Assert.True(plan.Checklist.Count >= 10);
    }
}
