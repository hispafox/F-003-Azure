using Apim.Demo.Api.Apim;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Apim.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4: los unit tests
// usan las clases puras directamente; este resuelve IApimPlanner del
// WebApplicationFactory real (sin Docker).
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void ApimPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IApimPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IApimPlanner>());

        var plan = planner.Planificar(
            new EscenarioApim(Produccion: true, RequiereVNet: true),
            new EscenarioUsoApim(MultiplesApis: true, ExponeATerceros: true));

        Assert.Equal(ApimTier.Premium, plan.Tier);
        Assert.True(plan.ApimRecomendado);
        Assert.Equal(EsquemaVersionado.Segment, plan.EsquemaVersionado);
        Assert.NotEmpty(plan.PoliciesInbound);
        Assert.NotEmpty(plan.Checklist);
    }
}
