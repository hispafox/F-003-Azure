using Plataforma.Demo.Api.Plataforma;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Plataforma.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PlatformPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPlatformPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPlatformPlanner>());

        var plan = planner.Planificar(
            new EscenarioPlataforma(YaUsasAdo: true,
                NecesitaBoardsCompletos: true, Personas: 8),
            new EscenarioCoste(8));

        Assert.Equal(TipoPlataforma.AzureDevOps, plan.Recomendacion.Plataforma);
        Assert.True(plan.EquivalenciasClave.Count >= 5);
        Assert.True(plan.Coste.Ado.TotalMes <= plan.Coste.Github.TotalMes);
        Assert.NotEmpty(plan.Checklist);
    }
}
