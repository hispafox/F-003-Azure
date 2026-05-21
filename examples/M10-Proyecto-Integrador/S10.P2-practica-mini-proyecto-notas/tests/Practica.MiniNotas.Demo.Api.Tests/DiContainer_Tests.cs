using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Practica.MiniNotas.Demo.Api.MiniNotas;

namespace Practica.MiniNotas.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaMiniNotasPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaMiniNotasPlanner>());

        var plan = planner.Planificar(new PlanRequest(
            Preflight: new EscenarioPreflight(
                TieneDotNet8SDK: true,
                TieneAzCli: true,
                TieneCurl: true,
                HizoM02: true,
                HizoM05: true),
            Evidencias:
            [
                new EvidenciaPaso(Paso.CrearSolucion, true, true),
                new EvidenciaPaso(Paso.EndpointsCrud, true, false),
            ],
            Objetivo: new EscenarioObjetivo(QuieresUnEndToEndMinimo: true)));

        Assert.True(plan.Preflight.ListoParaArrancar);
        Assert.Equal(2, plan.Pasos.Count);
        Assert.NotNull(plan.Alcance);
        Assert.Equal(Recomendacion.Mini, plan.Alcance!.Cual);
        Assert.True(plan.CaminoHaciaS101.Count >= 5);
        Assert.True(plan.Checklist.Count >= 10);
    }
}
