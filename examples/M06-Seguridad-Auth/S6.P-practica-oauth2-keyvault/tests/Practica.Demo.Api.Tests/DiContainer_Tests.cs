using Practica.Demo.Api.Practica;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Practica.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PracticaPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaPlanner>());

        var plan = planner.Planificar(TipoApp.Api, "t-1", "c-1", "kv-curso");
        Assert.Equal("Return401", plan.AccionEasyAuth);
        Assert.True(plan.SoloReferencias);
        Assert.NotEmpty(plan.Checklist);
    }
}
